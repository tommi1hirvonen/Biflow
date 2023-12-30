using Biflow.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Core;

internal class UpdateExecutionCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory) : IRequestHandler<UpdateExecutionCommand>
{
    public async Task Handle(UpdateExecutionCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Attach(request.Execution).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }
}
