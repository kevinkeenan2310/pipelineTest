using System;
using System.Collections.Generic;
using System.IO;
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
        /// This functtion will eventually become 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public string SkeletalLambda(string input, ILambdaContext context)
        {
            return input?.ToUpper();
        }

        public string QueryJobStatusLambda(string input, ILambdaContext context)
        {
            return "JobStatusLambdaTest";
        }
    }
}
