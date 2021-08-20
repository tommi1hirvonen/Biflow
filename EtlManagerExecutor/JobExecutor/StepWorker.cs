using EtlManagerDataAccess;
using EtlManagerDataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class StepWorker
    {
        private readonly IDbContextFactory<EtlManagerContext> _dbContextFactory;
        private readonly IServiceProvider _serviceProvider;

        private StepExecution StepExecution { get; init; }

        public StepWorker(IDbContextFactory<EtlManagerContext> dbContextFactory, IServiceProvider serviceProvider, StepExecution stepExecution)
        {
            _dbContextFactory = dbContextFactory;
            _serviceProvider = serviceProvider;
            StepExecution = stepExecution;
        }

        public async Task<bool> ExecuteStepAsync(ExtendedCancellationTokenSource cancellationTokenSource)
        {
            var cancellationToken = cancellationTokenSource.Token;

            // If the step was canceled already before it was even started, update the status to STOPPED.
            if (cancellationToken.IsCancellationRequested)
            {
                await UpdateExecutionStoppedAsync(cancellationTokenSource.Username);
                return false;
            }
            
            // Check whether this step is already running (in another execution). Only include executions from the past 24 hours.
            try
            {
                using var context = _dbContextFactory.CreateDbContext();
                var duplicateExecution = await IsDuplicateExecutionAsync(context);
                // This step execution should be marked as duplicate.
                if (duplicateExecution)
                {
                    await UpdateStepAsDuplicateAsync(context);
                    Log.Warning("{ExecutionId} {Step} Marked step as DUPLICATE", StepExecution.ExecutionId, StepExecution);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {Step} Error marking step as DUPLICATE", StepExecution.ExecutionId, StepExecution);
                return false;
            }

            IStepExecutor stepExecutor = StepExecution switch
            {
                SqlStepExecution sql => ActivatorUtilities.CreateInstance<SqlStepExecutor>(_serviceProvider, sql),
                PackageStepExecution package => ActivatorUtilities.CreateInstance<PackageStepExecutor>(_serviceProvider, package),
                JobStepExecution job => ActivatorUtilities.CreateInstance<JobStepExecutor>(_serviceProvider, job),
                PipelineStepExecution pipeline => ActivatorUtilities.CreateInstance<PipelineStepExecutor>(_serviceProvider, pipeline),
                ExeStepExecution exe => ActivatorUtilities.CreateInstance<ExeStepExecutor>(_serviceProvider, exe),
                DatasetStepExecution dataset => ActivatorUtilities.CreateInstance<DatasetStepExecutor>(_serviceProvider, dataset),
                FunctionStepExecution function => ActivatorUtilities.CreateInstance<FunctionStepExecutor>(_serviceProvider, function),
                _ => throw new InvalidOperationException($"{StepExecution.StepType} is not a recognized step type")
            };

            // Loop until there are not retry attempts left.
            while (stepExecutor.RetryAttemptCounter <= StepExecution.RetryAttempts)
            {
                await CheckIfStepExecutionIsRetryAttemptAsync(stepExecutor);

                // Execute the step based on its step type.
                Result result;
                try
                {
                    result = await stepExecutor.ExecuteAsync(cancellationTokenSource);
                }
                catch (OperationCanceledException)
                {
                    await UpdateExecutionCancelledAsync(stepExecutor, cancellationTokenSource.Username);
                    return false;
                }
                catch (Exception ex)
                {
                    result = Result.Failure("Error during step execution: " + ex.Message);
                }

                if (result is Failure failure)
                {
                    Log.Warning("{ExecutionId} {Step} Error executing step: " + failure.ErrorMessage, StepExecution.ExecutionId, StepExecution);
                    await UpdateExecutionFailedAsync(stepExecutor, failure);

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
                    Log.Information("{ExecutionId} {Step} Step executed successfully", StepExecution.ExecutionId, StepExecution);
                    // The step execution was successful. Update the execution accordingly.
                    await UpdateExecutionSucceededAsync(stepExecutor, result);
                    return true; // Break the loop to end this execution.
                }
            }

            return false; // Execution should not arrive here in normal conditions. Return false.
        }

        private async Task WaitForExecutionRetryAsync(IStepExecutor stepExecution, int retryIntervalMinutes, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(retryIntervalMinutes * 60 * 1000, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // If the step was canceled during waiting for a retry, copy a new execution row with STOPPED status.
                Log.Warning("{ExecutionId} {Step} Step was canceled", StepExecution.ExecutionId, StepExecution);
                try
                {
                    using var context = _dbContextFactory.CreateDbContext();
                    var prevAttempt = StepExecution.StepExecutionAttempts.OrderByDescending(e => e.RetryAttemptIndex).First();
                    var attempt = prevAttempt with
                    {
                        RetryAttemptIndex = stepExecution.RetryAttemptCounter,
                        ExecutionStatus = StepExecutionStatus.Stopped,
                        StartDateTime = DateTimeOffset.Now,
                        EndDateTime = DateTimeOffset.Now
                    };
                    attempt.Reset();
                    context.Attach(attempt).State = EntityState.Added;
                    await context.SaveChangesAsync(CancellationToken.None);
                    StepExecution.StepExecutionAttempts.Add(attempt);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "{ExecutionId} {Step} Error copying step execution details for retry attempt", StepExecution.ExecutionId, StepExecution);
                }
                throw;
            }
        }

        private async Task UpdateExecutionCancelledAsync(IStepExecutor stepExecution, string username)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var attempt = StepExecution.StepExecutionAttempts.First(e => e.RetryAttemptIndex == stepExecution.RetryAttemptCounter);
            attempt.EndDateTime = DateTimeOffset.Now;
            attempt.StoppedBy = username;
            attempt.ExecutionStatus = StepExecutionStatus.Stopped;
            context.Attach(attempt).State = EntityState.Modified;
            await context.SaveChangesAsync();
        }

        private async Task UpdateExecutionFailedAsync(IStepExecutor stepExecution, Failure failure)
        {
            // If there are attempts left, set the status to AWAIT RETRY. Otherwise set the status to FAILED.
            var status = stepExecution.RetryAttemptCounter >= StepExecution.RetryAttempts ? StepExecutionStatus.Failed : StepExecutionStatus.AwaitRetry;
            try
            {
                using var context = _dbContextFactory.CreateDbContext();
                var attempt = StepExecution.StepExecutionAttempts.First(e => e.RetryAttemptIndex == stepExecution.RetryAttemptCounter);
                attempt.ExecutionStatus = status;
                attempt.EndDateTime = DateTimeOffset.Now;
                attempt.ErrorMessage = failure.ErrorMessage;
                attempt.InfoMessage = failure.InfoMessage;
                context.Attach(attempt).State = EntityState.Modified;
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {Step} Error updating step status to {status}", StepExecution.ExecutionId, StepExecution, status);
            }
        }

        private async Task UpdateExecutionSucceededAsync(IStepExecutor stepExecution, Result executionResult)
        {
            try
            {
                using var context = _dbContextFactory.CreateDbContext();
                var attempt = StepExecution.StepExecutionAttempts.First(e => e.RetryAttemptIndex == stepExecution.RetryAttemptCounter);
                attempt.ExecutionStatus = StepExecutionStatus.Succeeded;
                attempt.EndDateTime = DateTimeOffset.Now;
                attempt.InfoMessage = executionResult.InfoMessage;
                context.Attach(attempt).State = EntityState.Modified;
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{ExecutionId} {Step} Error updating step status to SUCCEEDED", StepExecution.ExecutionId, StepExecution);
            }
        }

        private async Task UpdateExecutionStoppedAsync(string username)
        {
            using var context = _dbContextFactory.CreateDbContext();
            foreach (var attempt in StepExecution.StepExecutionAttempts)
            {
                attempt.ExecutionStatus = StepExecutionStatus.Stopped;
                attempt.StartDateTime = DateTimeOffset.Now;
                attempt.EndDateTime = DateTimeOffset.Now;
                attempt.StoppedBy = username;
                context.Attach(attempt).State = EntityState.Modified;
            }
            await context.SaveChangesAsync();
        }

        private async Task CheckIfStepExecutionIsRetryAttemptAsync(IStepExecutor stepExecution)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var attempt = StepExecution.StepExecutionAttempts.FirstOrDefault(e => e.RetryAttemptIndex == stepExecution.RetryAttemptCounter);
            if (attempt is not null)
            {
                attempt.StartDateTime = DateTimeOffset.Now;
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
                    StartDateTime = DateTimeOffset.Now,
                    EndDateTime = null
                };
                attempt.Reset();
                context.Attach(attempt).State = EntityState.Added;
                StepExecution.StepExecutionAttempts.Add(attempt);
            }
            await context.SaveChangesAsync();
        }

        private async Task<bool> IsDuplicateExecutionAsync(EtlManagerContext context)
        {
            var duplicate = await context.StepExecutionAttempts
                .Where(e => e.StepId == StepExecution.StepId && e.ExecutionStatus == StepExecutionStatus.Running && e.StartDateTime >= DateTimeOffset.Now.AddDays(-1))
                .AnyAsync();
            return duplicate;
        }

        private async Task UpdateStepAsDuplicateAsync(EtlManagerContext context)
        {
            foreach (var attempt in StepExecution.StepExecutionAttempts)
            {
                attempt.ExecutionStatus = StepExecutionStatus.Duplicate;
                attempt.StartDateTime = DateTimeOffset.Now;
                attempt.EndDateTime = DateTimeOffset.Now;
                context.Attach(attempt).State = EntityState.Modified;
            }
            await context.SaveChangesAsync();
        }

    }

}
