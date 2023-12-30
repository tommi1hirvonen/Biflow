using Biflow.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Core;

internal class UpdateConnectionCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory) : IRequestHandler<UpdateConnectionCommand>
{
    public async Task Handle(UpdateConnectionCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Attach(request.Connection).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }
}
