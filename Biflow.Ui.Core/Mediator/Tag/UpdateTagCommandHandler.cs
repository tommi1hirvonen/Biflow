using Biflow.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Core;

internal class UpdateTagCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory) : IRequestHandler<UpdateTagCommand>
{
    public async Task Handle(UpdateTagCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Attach(request.Tag).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }
}
