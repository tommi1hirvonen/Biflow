namespace Biflow.Ui.Core;

public record CreateDataTableCategoryCommand(MasterDataTableCategory Category) : IRequest;

internal class CreateDataTableCategoryCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateDataTableCategoryCommand>
{
    public async Task Handle(CreateDataTableCategoryCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.MasterDataTableCategories.Add(request.Category);
        await context.SaveChangesAsync(cancellationToken);
    }
}