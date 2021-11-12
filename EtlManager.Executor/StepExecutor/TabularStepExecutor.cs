using EtlManager.DataAccess;
using EtlManager.DataAccess.Models;
using Microsoft.AnalysisServices.Tabular;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManager.Executor;

public class TabularStepExecutor : StepExecutorBase
{
    private TabularStepExecution Step { get; }

    public TabularStepExecutor(
        IDbContextFactory<EtlManagerContext> dbContextFactory,
        TabularStepExecution step) : base(dbContextFactory, step)
    {
        Step = step;
    }

    protected override async Task<Result> ExecuteAsync(ExtendedCancellationTokenSource cancellationTokenSource)
    {
        var cancellationToken = cancellationTokenSource.Token;
        cancellationToken.ThrowIfCancellationRequested();

        using var timeoutCts = Step.TimeoutMinutes > 0
            ? new CancellationTokenSource(TimeSpan.FromMinutes(Step.TimeoutMinutes))
            : new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            await Task.Run(() =>
            {
                using var server = new Server();
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

                model.SaveChanges();
            }, linkedCts.Token);
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }

        return Result.Success();
    }
}
