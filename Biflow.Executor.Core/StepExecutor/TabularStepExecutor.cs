using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Microsoft.AnalysisServices.Tabular;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.StepExecutor;

internal class TabularStepExecutor(
    ILogger<TabularStepExecutor> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory,
    TabularStepExecution step) : StepExecutorBase(logger, dbContextFactory, step)
{
    private readonly TabularStepExecution _step = step;

    protected override async Task<Result> ExecuteAsync(ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        using var server = new Server();
        try
        {
            var refreshTask = Task.Run(() =>
            {       
                server.Connect(_step.Connection.ConnectionString);

                var database = server.Databases[_step.TabularModelName];
                var model = database.Model;

                if (!string.IsNullOrEmpty(_step.TabularTableName))
                {
                    var table = model.Tables[_step.TabularTableName];
                    if (!string.IsNullOrEmpty(_step.TabularPartitionName))
                    {
                        var partition = table.Partitions[_step.TabularPartitionName];
                        partition.RequestRefresh(RefreshType.Full);
                    }
                    else
                    {
                        table.RequestRefresh(RefreshType.Full);
                    }
                }
                else
                {
                    model.RequestRefresh(RefreshType.Full);
                }
                
                model.SaveChanges(); // This is a long running operation. RequestRefresh() returns immediately.
            });
            
            var timeoutTask = _step.TimeoutMinutes > 0
                ? Task.Delay(TimeSpan.FromMinutes(_step.TimeoutMinutes), cancellationToken)
                : Task.Delay(-1, cancellationToken);

            await Task.WhenAny(refreshTask, timeoutTask);

            if (!refreshTask.IsCompleted)
            {
                // The timeout task completed before the refresh task => step timed out.
                throw new OperationCanceledException();
            }
        }
        catch (OperationCanceledException ex)
        {
            await Task.Run(server.CancelCommand); // Cancel the SaveChanges operation.
            if (cancellationTokenSource.IsCancellationRequested)
            {
                AddWarning(ex);
                return Result.Cancel;
            }
            AddError(ex, "Step execution timed out");
            return Result.Failure;
        }
        catch (Exception ex)
        {
            AddError(ex, "Error processing tabular model");
            return Result.Failure;
        }

        return Result.Success;
    }
}
