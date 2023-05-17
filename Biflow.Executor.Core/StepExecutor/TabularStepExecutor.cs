using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Executor.Core.Common;
using Microsoft.AnalysisServices.Tabular;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.StepExecutor;

internal class TabularStepExecutor : StepExecutorBase
{
    private TabularStepExecution Step { get; }

    public TabularStepExecutor(
        ILogger<TabularStepExecutor> logger,
        IDbContextFactory<BiflowContext> dbContextFactory,
        TabularStepExecution step) : base(logger, dbContextFactory, step)
    {
        Step = step;
    }

    protected override async Task<Result> ExecuteAsync(ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        using var server = new Server();
        try
        {
            var refreshTask = Task.Run(() =>
            {       
                server.Connect(Step.Connection.ConnectionString);

                var database = server.Databases[Step.TabularModelName];
                var model = database.Model;

                if (!string.IsNullOrEmpty(Step.TabularTableName))
                {
                    var table = model.Tables[Step.TabularTableName];
                    if (!string.IsNullOrEmpty(Step.TabularPartitionName))
                    {
                        var partition = table.Partitions[Step.TabularPartitionName];
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
            
            var timeoutTask = Step.TimeoutMinutes > 0
                ? Task.Delay(TimeSpan.FromMinutes(Step.TimeoutMinutes), cancellationToken)
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
            return cancellationTokenSource.IsCancellationRequested
                ? new Cancel(ex)
                : new Failure(ex, "Step execution timed out");
        }
        catch (Exception ex)
        {
            return new Failure(ex, "Error processing tabular model");
        }

        return new Success();
    }
}
