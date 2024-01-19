namespace Biflow.Ui.Core;

public record CreateJobCommand(Job Job) : IRequest;

internal class CreateJobCommandHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<CreateJobCommand>
{
    public async Task Handle(CreateJobCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Jobs.Add(request.Job);
        await context.SaveChangesAsync(cancellationToken);
    }
}