using Biflow.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Core;

internal class DeleteTagRequestHandler(IDbContextFactory<AppDbContext> dbContextFactory) : IRequestHandler<DeleteTagRequest>
{
    public async Task Handle(DeleteTagRequest request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var tagToRemove = await context.Tags.FirstOrDefaultAsync(t => t.TagId == request.TagId, cancellationToken);
        if (tagToRemove is not null)
        {
            context.Tags.Remove(tagToRemove);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
