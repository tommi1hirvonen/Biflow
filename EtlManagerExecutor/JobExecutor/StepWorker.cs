using EtlManagerDataAccess;
using EtlManagerDataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class StepWorker
    {
        private ExecutionConfiguration Configuration { get; init; }
        private StepExecution StepExecution { get; init; }

        public StepWorker(ExecutionConfiguration executionConfiguration, StepExecution stepExecution)
        {
            Configuration = executionConfiguration;
            StepExecution = stepExecution;
        }

        public async Task<bool> ExecuteStepAsync(CancellationToken cancellationToken)
        {
            // If the step was canceled already before it was even started, update the status to STOPPED.
            if (cancellationToken.IsCancellationRequested)
            {
                await UpdateExecutionStoppedAsync();
                return false;
            }
            
            // Check whether this step is already running (in another execution). Only include executions from the past 24 hours.
            try
            {
                using var context = Configuration.DbContextFactory.CreateDbContext();
                var duplicateExecution = await IsDuplicateExecutionAsync(context);
                // This step execution should be marked as duplicate.
                if (duplicateExecution)
                {
                    await UpdateStepAsDuplicateAsync(context);
                    Log.Warning("{ExecutionId} {Step} Marked step as DUPLICATE", Configuration.ExecutionId, StepExecution);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {Step} Error marking step as DUPLICATE", Configuration.ExecutionId, StepExecution);
                return false;
            }

            StepExecutorBase stepExecutor = StepExecution switch
            {
                SqlStepExecution sql => new SqlStepExecutor(Configuration, sql),
                PackageStepExecution package => new PackageStepExecutor(Configuration, package),
                JobStepExecution job => new JobStepExecutor(Configuration, job),
                PipelineStepExecution pipeline => new PipelineStepExecutor(Configuration, pipeline),
                ExeStepExecution exe => new ExeStepExecutor(Configuration, exe),
                DatasetStepExecution dataset => new DatasetStepExecutor(Configuration, dataset),
                FunctionStepExecution function => new FunctionStepExecutor(Configuration, function),
                _ => throw new InvalidOperationException($"{StepExecution.StepType} is not a recognized step type")
            };

            // Loop until there are not retry attempts left.
            while (stepExecutor.RetryAttemptCounter <= StepExecution.RetryAttempts)
            {
                await CheckIfStepExecutionIsRetryAttemptAsync(stepExecutor);

                // Execute the step based on its step type.
                ExecutionResult executionResult;
                try
                {
                    executionResult = await stepExecutor.ExecuteAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    await UpdateExecutionCancelledAsync(stepExecutor);
                    return false;
                }
                catch (Exception ex)
                {
                    executionResult = new ExecutionResult.Failure("Error during step execution: " + ex.Message);
                }

                if (executionResult is ExecutionResult.Failure failureResult)
                {
                    Log.Warning("{ExecutionId} {Step} Error executing step: " + failureResult.ErrorMessage, Configuration.ExecutionId, StepExecution);
                    await UpdateExecutionFailedAsync(stepExecutor, failureResult);

                    // There are attempts left => increase counter and wait for the retry interval.
                    if (stepExecutor.RetryAttemptCounter < StepExecution.RetryAttempts)
                    {
                        stepExecutor.RetryAttemptCounter++;
                        try
                        {
                            await WaitForExecutionRetryAsync(stepExecutor, StepExecution.RetryIntervalMinutes, cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            return false;
                        }
                    }
                    // Otherwise break the loop and end this execution.
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    Log.Information("{ExecutionId} {Step} Step executed successfully", Configuration.ExecutionId, StepExecution);
                    // The step execution was successful. Update the execution accordingly.
                    await UpdateExecutionSucceededAsync(stepExecutor, executionResult);
                    return true; // Break the loop to end this execution.
                }
            }

            return false; // Execution should not arrive here in normal conditions. Return false.
        }

        private async Task WaitForExecutionRetryAsync(StepExecutorBase stepExecution, int retryIntervalMinutes, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(retryIntervalMinutes * 60 * 1000, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // If the step was canceled during waiting for a retry, copy a new execution row with STOPPED status.
                Log.Warning("{ExecutionId} {Step} Step was canceled", Configuration.ExecutionId, StepExecution);
                try
                {
                    using var context = Configuration.DbContextFactory.CreateDbContext();
                    var prevAttempt = StepExecution.StepExecutionAttempts.OrderByDescending(e => e.RetryAttemptIndex).First();
                    var attempt = prevAttempt with
                    {
                        RetryAttemptIndex = stepExecution.RetryAttemptCounter,
                        ExecutionStatus = StepExecutionStatus.Stopped,
                        StartDateTime = DateTime.Now,
                        EndDateTime = DateTime.Now,
                        ErrorMessage = null
                    };
                    // TODO: This does not belong here.
                    switch (attempt)
                    {
                        case SqlStepExecutionAttempt sql:
                            sql.InfoMessage = null;
                            break;
                        case ExeStepExecutionAttempt exe:
                            exe.InfoMessage = null;
                            break;
                        case FunctionStepExecutionAttempt function:
                            function.InfoMessage = null;
                            break;
                        default:
                            break;
                    }
                    context.Attach(attempt).State = EntityState.Added;
                    await context.SaveChangesAsync(CancellationToken.None);
                    StepExecution.StepExecutionAttempts.Add(attempt);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {Step} Error copying step execution details for retry attempt", Configuration.ExecutionId, StepExecution);
                }
                throw;
            }
        }

        private async Task UpdateExecutionCancelledAsync(StepExecutorBase stepExecution)
        {
            using var context = Configuration.DbContextFactory.CreateDbContext();
            var attempt = StepExecution.StepExecutionAttempts.First(e => e.RetryAttemptIndex == stepExecution.RetryAttemptCounter);
            attempt.EndDateTime = DateTime.Now;
            attempt.StoppedBy = Configuration.Username;
            attempt.ExecutionStatus = StepExecutionStatus.Stopped;
            context.Attach(attempt).State = EntityState.Modified;
            await context.SaveChangesAsync();
        }

        private async Task UpdateExecutionFailedAsync(StepExecutorBase stepExecution, ExecutionResult.Failure failureResult)
        {
            // If there are attempts left, set the status to AWAIT RETRY. Otherwise set the status to FAILED.
            var status = stepExecution.RetryAttemptCounter >= StepExecution.RetryAttempts ? StepExecutionStatus.Failed : StepExecutionStatus.AwaitRetry;
            try
            {
                using var context = Configuration.DbContextFactory.CreateDbContext();
                var attempt = StepExecution.StepExecutionAttempts.First(e => e.RetryAttemptIndex == stepExecution.RetryAttemptCounter);
                attempt.ExecutionStatus = status;
                attempt.EndDateTime = DateTime.Now;
                attempt.ErrorMessage = failureResult.ErrorMessage;
                // TODO: This does not belong here.
                switch (attempt)
                {
                    case SqlStepExecutionAttempt sql:
                        sql.InfoMessage = failureResult.InfoMessage;
                        break;
                    case ExeStepExecutionAttempt exe:
                        exe.InfoMessage = failureResult.InfoMessage;
                        break;
                    case FunctionStepExecutionAttempt function:
                        function.InfoMessage = failureResult.InfoMessage;
                        break;
                    default:
                        break;
                }
                context.Attach(attempt).State = EntityState.Modified;
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {Step} Error updating step status to {status}", Configuration.ExecutionId, StepExecution, status);
            }
        }

        private async Task UpdateExecutionSucceededAsync(StepExecutorBase stepExecution, ExecutionResult executionResult)
        {
            try
            {
                using var context = Configuration.DbContextFactory.CreateDbContext();
                var attempt = StepExecution.StepExecutionAttempts.First(e => e.RetryAttemptIndex == stepExecution.RetryAttemptCounter);
                attempt.ExecutionStatus = StepExecutionStatus.Succeeded;
                attempt.EndDateTime = DateTime.Now;
                // TODO: This does not belong here.
                switch (attempt)
                {
                    case SqlStepExecutionAttempt sql:
                        sql.InfoMessage = executionResult.InfoMessage;
                        break;
                    case ExeStepExecutionAttempt exe:
                        exe.InfoMessage = executionResult.InfoMessage;
                        break;
                    case FunctionStepExecutionAttempt function:
                        function.InfoMessage = executionResult.InfoMessage;
                        break;
                    default:
                        break;
                }
                context.Attach(attempt).State = EntityState.Modified;
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {Step} Error updating step status to SUCCEEDED", Configuration.ExecutionId, StepExecution);
            }
        }

        private async Task UpdateExecutionStoppedAsync()
        {
            using var context = Configuration.DbContextFactory.CreateDbContext();
            foreach (var attempt in StepExecution.StepExecutionAttempts)
            {
                attempt.ExecutionStatus = StepExecutionStatus.Stopped;
                attempt.StartDateTime = DateTime.Now;
                attempt.EndDateTime = DateTime.Now;
                attempt.StoppedBy = Configuration.Username;
                context.Attach(attempt).State = EntityState.Modified;
            }
            await context.SaveChangesAsync();
        }

        private async Task CheckIfStepExecutionIsRetryAttemptAsync(StepExecutorBase stepExecution)
        {
            using var context = Configuration.DbContextFactory.CreateDbContext();
            var attempt = StepExecution.StepExecutionAttempts.FirstOrDefault(e => e.RetryAttemptIndex == stepExecution.RetryAttemptCounter);
            if (attempt is not null)
            {
                attempt.StartDateTime = DateTime.Now;
                attempt.ExecutionStatus = StepExecutionStatus.Running;
                context.Attach(attempt).State = EntityState.Modified;
            }
            else
            {
                var prevAttempt = StepExecution.StepExecutionAttempts.OrderByDescending(e => e.RetryAttemptIndex).First();
                attempt = prevAttempt with
                {
                    RetryAttemptIndex = stepExecution.RetryAttemptCounter,
                    ExecutionStatus = StepExecutionStatus.Running,
                    StartDateTime = DateTime.Now,
                    EndDateTime = null,
                    ErrorMessage = null
                };
                // TODO: This does not belong here.
                switch (attempt)
                {
                    case SqlStepExecutionAttempt sql:
                        sql.InfoMessage = null;
                        break;
                    case ExeStepExecutionAttempt exe:
                        exe.InfoMessage = null;
                        break;
                    case FunctionStepExecutionAttempt function:
                        function.InfoMessage = null;
                        break;
                    default:
                        break;
                }
                context.Attach(attempt).State = EntityState.Added;
                StepExecution.StepExecutionAttempts.Add(attempt);
            }
            await context.SaveChangesAsync();
        }

        private async Task<bool> IsDuplicateExecutionAsync(EtlManagerContext context)
        {
            var duplicate = await context.StepExecutionAttempts
                .Where(e => e.StepId == StepExecution.StepId && e.ExecutionStatus == StepExecutionStatus.Running && e.StartDateTime >= DateTime.Now.AddDays(-1))
                .AnyAsync();
            return duplicate;
        }

        private async Task UpdateStepAsDuplicateAsync(EtlManagerContext context)
        {
            foreach (var attempt in StepExecution.StepExecutionAttempts)
            {
                attempt.ExecutionStatus = StepExecutionStatus.Duplicate;
                attempt.StartDateTime = DateTime.Now;
                attempt.EndDateTime = DateTime.Now;
                context.Attach(attempt).State = EntityState.Modified;
            }
            await context.SaveChangesAsync();
        }

    }

}
