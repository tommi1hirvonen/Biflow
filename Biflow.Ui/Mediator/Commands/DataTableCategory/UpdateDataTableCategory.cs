namespace Biflow.Ui;

public record UpdateDataTableCategoryCommand(MasterDataTableCategory Category) : IRequest;

internal class UpdateDataTableCategoryCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateDataTableCategoryCommand>
{
    public async Task Handle(UpdateDataTableCategoryCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.MasterDataTableCategories.Attach(request.Category).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }
}