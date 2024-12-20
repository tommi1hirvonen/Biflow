using Biflow.Executor.Core.Common;
using Microsoft.AnalysisServices.Tabular;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.StepExecutor;

[UsedImplicitly]
internal class TabularStepExecutor(
    ILogger<TabularStepExecutor> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory)
    : StepExecutor<TabularStepExecution, TabularStepExecutionAttempt>(logger, dbContextFactory)
{
    protected override async Task<Result> ExecuteAsync(
        TabularStepExecution step,
        TabularStepExecutionAttempt attempt,
        ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        var connection = step.GetConnection();
        ArgumentNullException.ThrowIfNull(connection);

        using var server = new Server();
        try
        {
            Task RefreshDelegate() => Task.Run(() =>
            {
                server.Connect(connection.ConnectionString);
                var database = server.Databases[step.TabularModelName];
                var model = database.Model;

                if (!string.IsNullOrEmpty(step.TabularTableName))
                {
                    var table = model.Tables[step.TabularTableName];
                    if (!string.IsNullOrEmpty(step.TabularPartitionName))
                    {
                        var partition = table.Partitions[step.TabularPartitionName];
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

                model.SaveChanges(); // This is a long-running operation. RequestRefresh() returns immediately.
            }, CancellationToken.None);

            var refreshTask = connection.RunImpersonatedOrAsCurrentUserAsync(RefreshDelegate);

            var timeoutTask = step.TimeoutMinutes > 0
                ? Task.Delay(TimeSpan.FromMinutes(step.TimeoutMinutes), cancellationToken)
                : Task.Delay(-1, cancellationToken);

            await Task.WhenAny(refreshTask, timeoutTask);

            if (!refreshTask.IsCompleted)
            {
                // The timeout task completed before the refresh task => step timed out.
                throw new OperationCanceledException();
            }

            // Refresh task finished, await it to get possible error.
            await refreshTask;
        }
        catch (OperationCanceledException ex)
        {
            // Cancel the SaveChanges operation.
            await connection.RunImpersonatedOrAsCurrentUserAsync(
                () => Task.Run(server.CancelCommand, CancellationToken.None));
            if (cancellationTokenSource.IsCancellationRequested)
            {
                attempt.AddWarning(ex);
                return Result.Cancel;
            }
            attempt.AddError(ex, "Step execution timed out");
            return Result.Failure;
        }
        catch (Exception ex)
        {
            attempt.AddError(ex, "Error processing tabular model");
            return Result.Failure;
        }

        return Result.Success;
    }
}
