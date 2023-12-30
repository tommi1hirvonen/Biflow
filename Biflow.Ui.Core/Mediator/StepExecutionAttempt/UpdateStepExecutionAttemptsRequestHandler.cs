using Biflow.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Core;

internal class UpdateStepExecutionAttemptsRequestHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateStepExecutionAttemptsRequest>
{
    public async Task Handle(UpdateStepExecutionAttemptsRequest request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        foreach (var attempt in request.Attempts)
        {
            context.Attach(attempt).State = EntityState.Modified;
        }
        await context.SaveChangesAsync(cancellationToken);
    }
}
