namespace Biflow.Ui.Api.Mediator.Commands;

public record UpdateDataTableCategoryCommand(
    Guid CategoryId,
    string CategoryName) : IRequest<MasterDataTableCategory>;

[UsedImplicitly]
internal class UpdateDataTableCategoryCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateDataTableCategoryCommand, MasterDataTableCategory>
{
    public async Task<MasterDataTableCategory> Handle(UpdateDataTableCategoryCommand request,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var category = await dbContext.MasterDataTableCategories
            .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId, cancellationToken)
            ?? throw new NotFoundException<MasterDataTableCategory>(request.CategoryId);
        
        category.CategoryName = request.CategoryName;
        
        category.EnsureDataAnnotationsValidated();
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return category;
    }
}