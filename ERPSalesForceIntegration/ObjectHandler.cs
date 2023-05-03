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
using Microsoft.Extensions.Logging;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;

namespace ERPSalesForceIntegration
{
    public class ObjectScheduler
    {
        private static readonly IAmazonCloudWatchLogs CloudWatchLogs = new AmazonCloudWatchLogsClient();
        private const string TargetFunctionName = "SkeletalLambda";
        //private const string TargetFunctionInput = "{\"objectTypeKey\": \"salesforceAbcCodes\"}";
        public ILogger<ObjectScheduler> _logger;

        public ObjectScheduler()
        {
            LambdaLoggerOptions loggerOptions = new LambdaLoggerOptions()
            {
                IncludeException = true
            };
            ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddLambdaLogger(loggerOptions);
            });
            _logger = loggerFactory.CreateLogger<ObjectScheduler>();
        }
        public async Task<string> SchedulerFunction(ILambdaContext context)
        {
            string timestamp = DateTime.Now.Ticks.ToString();
            string logStreamName = $"log-stream-{timestamp}";

            // Set the log stream name for this invocation
            System.Environment.SetEnvironmentVariable("AWS_LAMBDA_LOG_STREAM_NAME", logStreamName);

            // Log some data using ILogger
            _logger.LogInformation("Hello, world!");

            // Create a new log stream for the next invocation
            await CloudWatchLogs.CreateLogStreamAsync(new CreateLogStreamRequest
            {
                LogGroupName = context.LogGroupName,
                LogStreamName = logStreamName
            });

            _logger.LogInformation("Starting the lookup scheduler function ");
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

            _logger.LogInformation("Invoking tasks");
            // invoke the Lambda functions concurrently using Task.WhenAll
            var invokeTasks = invokeRequests.Select(invokeRequest => lambdaClient.InvokeAsync(invokeRequest)).ToArray();
            await Task.WhenAll(invokeTasks);

            // process the responses from the Lambda functions
            var responseStrings = invokeTasks.Select(invokeTask => Encoding.UTF8.GetString(invokeTask.Result.Payload.ToArray()));
            foreach (var responseString in responseStrings)
            {
                _logger.LogInformation($"response from each call: {responseString}");
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