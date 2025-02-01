namespace Biflow.Ui.Api.Mediator.Commands;

public record CreateDataTableCategoryCommand(string CategoryName) : IRequest<MasterDataTableCategory>;

[UsedImplicitly]
internal class CreateDataTableCategoryCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateDataTableCategoryCommand, MasterDataTableCategory>
{
    public async Task<MasterDataTableCategory> Handle(CreateDataTableCategoryCommand request,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var category = new MasterDataTableCategory { CategoryName = request.CategoryName };
        
        category.EnsureDataAnnotationsValidated();
        
        dbContext.MasterDataTableCategories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return category;
    }
}