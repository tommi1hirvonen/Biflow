using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Core;

public record UpdateDataTableCategoryCommand(MasterDataTableCategory Category) : IRequest;

internal class UpdateDataTableCategoryCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateDataTableCategoryCommand>
{
    public async Task Handle(UpdateDataTableCategoryCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.MasterDataTableCategories.Attach(request.Category).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }
}