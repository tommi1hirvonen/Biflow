using Biflow.Executor.Core.Common;
using Microsoft.AnalysisServices.Tabular;

namespace Biflow.Executor.Core.StepExecutor;

internal class TabularStepExecutor(TabularStepExecution step) : IStepExecutor<TabularStepExecutionAttempt>
{
    private readonly TabularStepExecution _step = step;
    private readonly AnalysisServicesConnectionInfo _connection = step.GetConnection()
        ?? throw new ArgumentNullException(nameof(_connection));

    public TabularStepExecutionAttempt Clone(TabularStepExecutionAttempt other, int retryAttemptIndex) =>
        new(other, retryAttemptIndex);

    public async Task<Result> ExecuteAsync(TabularStepExecutionAttempt attempt, ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        using var server = new Server();
        try
        {
            var refreshTask = Task.Run(() =>
            {       
                server.Connect(_connection.ConnectionString);

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
