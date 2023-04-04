using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon;
using Amazon.Lambda.Core;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using ERPSalesForceIntegration.Models.Secrets;
using ERPSalesForceIntegration.Models.Response;

namespace ERPSalesForceIntegration
{
    public class SalesforceAuthHandler
    {
        private static string region = "us-east-1";
        private static string authSecretName = "CustomerRelationshipManagementApi/SalesforceAuthenticationToken";

        /// <summary>
        /// Refreshes Salesforce Access Token stored in AWS
        /// </summary>
        public async Task<SalesforceAuthHandlerResponse> AccessTokenHandler(ILambdaContext context)
        {
            SalesforceAuthHandlerResponse salesforceAuthResponse = new SalesforceAuthHandlerResponse();
            try
            {
                SalesforceAuthSecret authSecret = GetAuthSecret();
                HttpClient client = AssembleHttpClient(authSecret);
                await RevokeAccessTokenFromSalesforce(client, authSecret.accessToken);

                RefreshAccessTokenResponse newAccessCodeResponse = await RefreshAccessTokenFromSalesforce(client, authSecret);

                authSecret.accessToken = newAccessCodeResponse.Access_Token;
                await UpdateSecretWithNewAccessToken(authSecret);

                salesforceAuthResponse.IsSuccess = true;
                salesforceAuthResponse.Message = "The salesforce access token and it's corresponding AWS secret have been succesfully updated.";
            }
            catch (Exception e)
            {
                salesforceAuthResponse.IsSuccess = false;
                salesforceAuthResponse.Message = e.Message;
            }
            return salesforceAuthResponse;
        }

        /// <summary>
        /// Helper method to retrieve the salesforce secret
        /// </summary>
        /// <returns>SalesforceAuthSecret</returns>
        private SalesforceAuthSecret GetAuthSecret()
        {
            string authSecretResponse = GetSecret(authSecretName).Result;
            SalesforceAuthSecret authSecret = new SalesforceAuthSecret();

            try
            {
                authSecret = JsonConvert.DeserializeObject<SalesforceAuthSecret>(authSecretResponse);
            }
            catch(Exception e)
            {
                throw new Exception("Error while deserializing AWS Secret.  Please ensure the AWS secret name, as well as the field names have not changed.  The exception message is: " + e.Message); 
            }

            return authSecret;
        }

        /// <summary>
        /// Helper method to get the secret value given a secretName
        /// </summary>
        /// <returns>A string containing the secret keys and values</returns>
        private async Task<string> GetSecret(string secretName)
        {
            IAmazonSecretsManager client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));

            GetSecretValueRequest request = new GetSecretValueRequest
            {
                SecretId = secretName
            };

            GetSecretValueResponse response;
            try
            {
                response = await client.GetSecretValueAsync(request);
                return response.SecretString;
            }
            catch (Exception e)
            {
                throw new Exception("Error retrieving secret: + " + secretName + ". The exception message is: " + e.Message);
            }
        }

        /// <summary>
        /// Creates a client to be used to request salesforce
        /// </summary>
        /// <returns>HttpClient ready to interact with salesforce </returns>
        private HttpClient AssembleHttpClient(SalesforceAuthSecret authSecret)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(authSecret.url);
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + authSecret.accessToken);
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        /// <summary>
        /// Revokes access token from salesforce, must be done if you want to refresh the token.  Although you will not see this in the response from this method, expect a 40x response from salesforce if the token no longer exists.  That is a normal, expected occurence, that happens whenever an access token is expired.
        /// </summary>
        public async Task RevokeAccessTokenFromSalesforce(HttpClient client, string accessToken) 
        {
            string revokePath = "/services/oauth2/revoke";
            Dictionary<string, string> revokeTokenDict = new Dictionary<string, string>
            {
                {"token", accessToken}
            };

            try
            {
                var revokeTokenRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(client.BaseAddress + revokePath)) { Content = new FormUrlEncodedContent(revokeTokenDict) };
                var response = await client.SendAsync(revokeTokenRequest);
                if(response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new Exception("The URL path in your secret is incorrect and needs to be updated or salesforce has updated the revoke path.  Please refer to the documentation for updating the URL in the secrets manager and verify that the revoke path for salesforce is still: " + revokePath + ".");
                }
            }
            catch (Exception e)
            {
                    throw new Exception("Error while making call to revoke access token.  The exception message is: " + e.Message);
            }
        }

        /// <summary>
        /// Refreshses access token in salesforce.  Token must be revoked before hand.  If it is not revoked or expired, your current token will be returned.  
        /// </summary>
        /// <returns>RefreshAccessTokenResponse </returns>
        public async Task<RefreshAccessTokenResponse> RefreshAccessTokenFromSalesforce(HttpClient client, SalesforceAuthSecret authSecret)
        {
            string refreshPath = "/services/oauth2/token";
            RefreshAccessTokenResponse content = new RefreshAccessTokenResponse();
            Dictionary<string, string> refreshTokenDict = new Dictionary<string, string>
            {
                {"grant_type", "refresh_token"},
                {"client_id", authSecret.consumerKey },
                {"client_secret", authSecret.consumerSecret },
                {"refresh_token", authSecret.refreshToken },
            };

            try
            {
                var refreshTokenRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(client.BaseAddress + refreshPath)) { Content = new FormUrlEncodedContent(refreshTokenDict) };
                var refreshTokenResponse = await client.SendAsync(refreshTokenRequest);
                var refreshTokenResponseContent = await refreshTokenResponse.Content.ReadAsStringAsync();
                content = JsonConvert.DeserializeObject<RefreshAccessTokenResponse>(refreshTokenResponseContent);

                if (!refreshTokenResponse.IsSuccessStatusCode)
                {
                    throw new Exception("Error most likely due to incorrect values in the secret  I.E. The A) Grant Type B) client_id C) client_secret D) refresh_token or E) URL.  Please follow the documentation provided for this lambda in order to manually update the secret and then reattempt the process. The error returned from salesforce is '" + content.Error + ": " + content.Error_Description + "';");
                }
            }
            catch(Exception e)
            {
                throw new Exception("Error refreshing Access Token From Salesforce, this is likely an issue relating to the following, either A) An issue exists within the lambda code associated with refreshing the access token. B) The secret properties have been renamed or values are incorrect C) The url key is incorrect within the secret.  The exception message is: " + e.Message);
            }

            return content;
        }

        /// <summary>
        /// Updates the salesforce authentication secret in AWS
        /// </summary>
        /// <returns></returns>
        private async Task UpdateSecretWithNewAccessToken(SalesforceAuthSecret authSecret)
        {
            IAmazonSecretsManager client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));

            PutSecretValueResponse response;
            try
            {
                PutSecretValueRequest request = new PutSecretValueRequest
                {
                    SecretId = authSecretName,
                    SecretString = JsonConvert.SerializeObject(authSecret)
                };
                response = await client.PutSecretValueAsync(request); // throws exception if failure, i.e. bad secret name or bad vals
            }
            catch (LimitExceededException e)
            {
                throw new Exception("Please do not run this lambda more than once every 10 minutes.  AWS secrets does not recommend updating a secret in short intervals.  The exception message is: " + e.Message);
            }
            catch (Exception e)
            {
                throw new Exception("Error persisting updated auth secret, ensure that your secret name is correct/unchanged, the field names within the secret have not changed, and that permissions are correctly configured with the AWS account/services.  Please manually update the secret with new access tokens (using the documentation) and re run the process.  The exception message is: " + e.Message);
            }
        }
    }
}