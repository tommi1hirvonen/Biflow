namespace Biflow.Ui.Core;

public record DeleteDataTableCategoryCommand(Guid CategoryId) : IRequest;

[UsedImplicitly]
internal class DeleteDataTableCategoryCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteDataTableCategoryCommand>
{
    public async Task Handle(DeleteDataTableCategoryCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var category = await context.MasterDataTableCategories
            .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId, cancellationToken)
            ?? throw new NotFoundException<MasterDataTableCategory>(request.CategoryId);
        context.MasterDataTableCategories.Remove(category);
        await context.SaveChangesAsync(cancellationToken);
    }
}