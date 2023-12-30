using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Core;

internal class DeleteExecutionRequestHandler(IDbContextFactory<AppDbContext> dbContextFactory) : IRequestHandler<DeleteExecutionRequest>
{
    public async Task Handle(DeleteExecutionRequest request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var execution = await context.Executions
                .Include(e => e.ExecutionParameters)
                .Include(e => e.StepExecutions)
                .ThenInclude(e => e.ExecutionDependencies)
                .Include(e => e.StepExecutions)
                .ThenInclude(e => e.DependantExecutions)
                .Include($"{nameof(Execution.StepExecutions)}.{nameof(IHasStepExecutionParameters.StepExecutionParameters)}.{nameof(StepExecutionParameterBase.ExpressionParameters)}")
                .FirstOrDefaultAsync(e => e.ExecutionId == request.ExecutionId, cancellationToken);
        if (execution is not null)
        {
            context.Executions.Remove(execution);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
