using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ERPSalesForceIntegration
{
    public class ExtractTransformLoadErpObjects
    {
        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// This functtion will eventually be expanded to pull from erp API and upsert records into salesforce
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<string> SkeletalLambda(string input, ILambdaContext context)
        {
            return input.ToUpper();
        }

        /// <summary>
        /// Another simple function created in order to test the creation of mulitple aws resources from template.yaml file
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<string> SkeletalLambda2(string input, ILambdaContext context)
        {
            return "Testing a lambda projects second function";
        }
    }
}