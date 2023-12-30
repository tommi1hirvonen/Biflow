using Biflow.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Core;

internal class UpdateExecutionRequestHandler(IDbContextFactory<AppDbContext> dbContextFactory) : IRequestHandler<UpdateExecutionRequest>
{
    public async Task Handle(UpdateExecutionRequest request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Attach(request.Execution).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }
}
