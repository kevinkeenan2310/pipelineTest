using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Newtonsoft.Json;
using Amazon.Lambda.Model;
using Amazon.Lambda;
using Amazon.CloudWatchEvents;
using Amazon.CloudWatchEvents.Model;
using Amazon.Lambda.Serialization.SystemTextJson;
using System.Linq;
using MySqlConnector;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace ERPSalesForceIntegration
{
    public class ObjectScheduler
    {
        private const string TargetFunctionName = "SkeletalLambda";
        //private const string TargetFunctionInput = "{\"objectTypeKey\": \"salesforceAbcCodes\"}";


        public async Task<string> SchedulerFunction(ILambdaContext context)
        {
            var lambdaClient = new AmazonLambdaClient();

            //var payloadStr = "salesforceAbcCodes";

            // define an array of parameters for each invocation
            var parameters = new string[] { "salesforceAbcCodes", "salesforceBuyingGroups", "salesforceCostCenters", "salesforceCustomerTypes", "salesforceItemClass", "salesforceItemCodes", "salesforcePaymentTerms", "salesforceProductLines" };

            // create an array of InvokeRequest objects for each invocation
            var invokeRequests = parameters.Select(parameter => new InvokeRequest
            {
                FunctionName = TargetFunctionName,
                Payload = "\"" + parameter + "\""
            }).ToArray();

            // invoke the Lambda functions concurrently using Task.WhenAll
            var invokeTasks = invokeRequests.Select(invokeRequest => lambdaClient.InvokeAsync(invokeRequest)).ToArray();
            await Task.WhenAll(invokeTasks);

            // process the responses from the Lambda functions
            var responseStrings = invokeTasks.Select(invokeTask => Encoding.UTF8.GetString(invokeTask.Result.Payload.ToArray()));
            foreach (var responseString in responseStrings)
            {
                Console.WriteLine(responseString);
            }
            return responseStrings.ToArray()[0];

            //var cronExpression = "0 6 * * ?";

            //var ruleName = $"{TargetFunctionName}-daily-rule";
            //var targets = new[]
            //{
            //    new Target
            //    {
            //        Arn = lambdaClient.GetFunctionConfigurationAsync(new GetFunctionConfigurationRequest
            //        {
            //            FunctionName = TargetFunctionName
            //        }).Result.FunctionArn,
            //        Input = TargetFunctionInput
            //    }
            //};

            //try
            //{
            //    var cloudWatchEventsClient = new AmazonCloudWatchEventsClient();
            //    await cloudWatchEventsClient.PutRuleAsync(new PutRuleRequest
            //    {
            //        Name = ruleName,
            //        ScheduleExpression = cronExpression,
            //        State = RuleState.ENABLED
            //    });

            //    await cloudWatchEventsClient.PutTargetsAsync(new PutTargetsRequest
            //    {
            //        Rule = ruleName,
            //        Targets = targets.ToList<Target>()
            //    });
            //}
            //catch (Exception ex)
            //{
            //    context.Logger.LogLine($"Error scheduling Lambda function: {ex.Message}");
            //}
            //return "";
        }
    }

}