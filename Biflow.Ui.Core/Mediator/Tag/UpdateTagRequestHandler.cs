using Biflow.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Core;

internal class UpdateTagRequestHandler(IDbContextFactory<AppDbContext> dbContextFactory) : IRequestHandler<UpdateTagRequest>
{
    public async Task Handle(UpdateTagRequest request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Attach(request.Tag).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }
}
