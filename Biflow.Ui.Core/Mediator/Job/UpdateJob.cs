using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Core;

public record UpdateJobCommand(Job Job) : IRequest;

internal class UpdateJobCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory) : IRequestHandler<UpdateJobCommand>
{
    public async Task Handle(UpdateJobCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.Jobs.AsQueryable();
        if (request.Job.JobConcurrencies is not null)
        {
            query = query.Include(c => c.JobConcurrencies);
        }
        var job = await query.FirstOrDefaultAsync(j => j.JobId == request.Job.JobId, cancellationToken);
        if (job is null)
        {
            return;
        }
        context.Entry(job).CurrentValues.SetValues(request.Job);
        if (request.Job.JobConcurrencies is not null)
        {
            context.MergeCollections(job.JobConcurrencies, request.Job.JobConcurrencies, c => c.StepType);
        }
        await context.SaveChangesAsync(cancellationToken);
    }
}