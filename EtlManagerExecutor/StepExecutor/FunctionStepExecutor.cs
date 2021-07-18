using EtlManagerDataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class FunctionStepExecutor : StepExecutorBase
    {
        private FunctionStepExecution Step { get; init; }

        private const int MaxRefreshRetries = 3;

        private JsonSerializerOptions JsonSerializerOptions { get; } = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public FunctionStepExecutor(ExecutionConfiguration configuration, FunctionStepExecution step) : base(configuration)
        {
            Step = step;
        }

        public override async Task<ExecutionResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            FunctionApp functionApp;
            try
            {
                using var context = Configuration.DbContextFactory.CreateDbContext();
                functionApp = await context.FunctionApps.FindAsync(Step.FunctionAppId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {Step} Error reading FunctionApp object from database", Configuration.ExecutionId, Step);
                return new ExecutionResult.Failure($"Error reading FunctionApp object from database: {ex.Message}");
            }
            
            var functionUrl = functionApp.FunctionAppUrl + "/" + Step.FunctionName;
            var message = new HttpRequestMessage(HttpMethod.Post, functionUrl);

            // Add function app security code as a request header.
            if (!string.IsNullOrEmpty(functionApp.FunctionAppKey))
            {
                message.Headers.Add("x-functions-key", functionApp.FunctionAppKey);
            }
            
            // If the input for the function was defined, add it to the request content.
            if (!string.IsNullOrEmpty(Step.FunctionInput))
            {
                message.Content = new StringContent(Step.FunctionInput);
            }
            
            var client = new HttpClient();
            var startTime = DateTime.Now;
            HttpResponseMessage response;
            string content;

            // Send the request to the function url. This will start the function, if the request was successful.
            try
            {
                response = await client.SendAsync(message, cancellationToken);
                content = await response.Content.ReadAsStringAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                return new ExecutionResult.Failure($"Error sending POST request to start function: {ex.Message}");
            }

            if (response.IsSuccessStatusCode)
            {
                ExecutionResult executionResult;
                if (Step.FunctionIsDurable)
                {
                    executionResult = await HandleDurableFunctionPolling(client, content, startTime, cancellationToken);
                }
                else
                {
                    executionResult = new ExecutionResult.Success(content);
                }

                return executionResult;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
            {
                return new ExecutionResult.Failure("Error executing function (500 Internal Server Error)");
            }
            else
            {
                return new ExecutionResult.Failure($"Error sending POST request to start function: {content}");
            }
        }

        private async Task<ExecutionResult> HandleDurableFunctionPolling(HttpClient client, string content, DateTime startTime, CancellationToken cancellationToken)
        {
            var startResponse = JsonSerializer.Deserialize<StartResponse>(content, JsonSerializerOptions)
                    ?? throw new InvalidOperationException("Start response was null");

            // Update instance id for the step execution attempt
            try
            {
                using var context = Configuration.DbContextFactory.CreateDbContext();
                var attempt = Step.StepExecutionAttempts.FirstOrDefault(e => e.RetryAttemptIndex == RetryAttemptCounter);
                if (attempt is not null && attempt is FunctionStepExecutionAttempt function)
                {
                    function.FunctionInstanceId = startResponse.Id;
                    context.Attach(function);
                    context.Entry(function).Property(e => e.FunctionInstanceId).IsModified = true;
                    await context.SaveChangesAsync(CancellationToken.None);
                }
                else
                {
                    throw new InvalidOperationException("Could not find step execution attempt to update function instance id");
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "{ExecutionId} {Step} Error updating function instance id", Configuration.ExecutionId, Step);
            }

            StatusResponse status;
            while (true)
            {
                try
                {
                    status = await TryGetStatusAsync(client, startResponse.StatusQueryGetUri, cancellationToken);
                    if (status.RuntimeStatus == "Pending" || status.RuntimeStatus == "Running" || status.RuntimeStatus == "ContinuedAsNew")
                    {
                        // Check for timeout.
                        if (Step.TimeoutMinutes > 0 && (DateTime.Now - startTime).TotalMinutes > Step.TimeoutMinutes)
                        {
                            await CancelAsync(client, startResponse.TerminatePostUri, "StepTimedOut");
                            Log.Warning("{ExecutionId} {Step} Step execution timed out", Configuration.ExecutionId, Step);
                            return new ExecutionResult.Failure("Step execution timed out");
                        }

                        await Task.Delay(Configuration.PollingIntervalMs, cancellationToken);
                    }
                    else
                    {
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    await CancelAsync(client, startResponse.TerminatePostUri, "StepWasCanceled");
                    throw;
                }
            }

            if (status.RuntimeStatus == "Completed")
            {
                return new ExecutionResult.Success(status.Output.ToString());
            }
            else if (status.RuntimeStatus == "Terminated")
            {
                return new ExecutionResult.Failure($"Function was terminated: {status.Output}");
            }
            else
            {
                return new ExecutionResult.Failure($"Function failed: {status.Output}");
            }
        }

        private async Task CancelAsync(HttpClient client, string terminateUrl, string reason)
        {
            try
            {
                var url = terminateUrl.Replace("{text}", reason);
                var response = await client.PostAsync(url, null!);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {Step} Error stopping function ", Configuration.ExecutionId, Step);
            }
        }

        private async Task<StatusResponse> TryGetStatusAsync(HttpClient client, string statusUrl, CancellationToken cancellationToken)
        {
            int refreshRetries = 0;
            while (refreshRetries < MaxRefreshRetries)
            {
                try
                {
                    var response = await client.GetAsync(statusUrl, CancellationToken.None);
                    var content = await response.Content.ReadAsStringAsync(CancellationToken.None);
                    var statusResponse = JsonSerializer.Deserialize<StatusResponse>(content, JsonSerializerOptions)
                        ?? throw new InvalidOperationException("Status response was null");
                    return statusResponse;
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "{ExecutionId} {Step} Error getting function instance status", Configuration.ExecutionId, Step);
                    refreshRetries++;
                    await Task.Delay(Configuration.PollingIntervalMs, cancellationToken);
                }
            }
            throw new TimeoutException("The maximum number of function instance status refresh attempts was reached.");
        }

        private record StartResponse(string Id, string StatusQueryGetUri, string SendEventPostUri, string TerminatePostUri, string PurgeHistoryDeleteUri);

        private record StatusResponse(string Name, string InstanceId, string RuntimeStatus, JsonElement? Input, JsonElement? CustomStatus,
            JsonElement? Output, DateTime CreatedTime, DateTime LastUpdatedTime);

    }
}
