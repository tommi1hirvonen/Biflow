namespace Biflow.Ui.Core;

public record UpdateJobCommand(Job Job) : IRequest;

internal class UpdateJobCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory) : IRequestHandler<UpdateJobCommand>
{
    public async Task Handle(UpdateJobCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var job = await context.Jobs.FirstOrDefaultAsync(j => j.JobId == request.Job.JobId, cancellationToken);
        if (job is null)
        {
            return;
        }
        context.Entry(job).CurrentValues.SetValues(request.Job);
        await context.SaveChangesAsync(cancellationToken);
    }
}