namespace Biflow.Ui.Core;

public record CreateJobCategoryCommand(JobCategory Category) : IRequest;

internal class CreateJobCategoryCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateJobCategoryCommand>
{
    public async Task Handle(CreateJobCategoryCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.JobCategories.Add(request.Category);
        await context.SaveChangesAsync(cancellationToken);
    }
}