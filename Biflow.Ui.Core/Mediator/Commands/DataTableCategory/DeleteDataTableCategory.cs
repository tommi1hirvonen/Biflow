namespace Biflow.Ui.Core;

public record DeleteDataTableCategoryCommand(Guid CategoryId) : IRequest;

internal class DeleteDataTableCategoryCommandHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<DeleteDataTableCategoryCommand>
{
    public async Task Handle(DeleteDataTableCategoryCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var category = await context.MasterDataTableCategories
            .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId, cancellationToken);
        if (category is not null)
        {
            context.MasterDataTableCategories.Remove(category);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}