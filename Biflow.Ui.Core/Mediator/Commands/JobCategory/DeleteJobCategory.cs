namespace Biflow.Ui.Core;

public record DeleteJobCategoryCommand(Guid CategoryId) : IRequest;

internal class DeleteJobCategoryCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteJobCategoryCommand>
{
    public async Task Handle(DeleteJobCategoryCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var category = await context.JobCategories
            .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId, cancellationToken);
        if (category is not null)
        {
            context.JobCategories.Remove(category);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}