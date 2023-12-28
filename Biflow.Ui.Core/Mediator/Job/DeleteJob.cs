using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Core;

public record DeleteJobRequest(Guid JobId) : IRequest;

public class DeleteJobRequestHandler(IDbContextFactory<AppDbContext> dbContextFactory) : IRequestHandler<DeleteJobRequest>
{
    public async Task Handle(DeleteJobRequest request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var jobToRemove = await context.Jobs
            .Include(j => j.JobParameters)
            .ThenInclude(j => j.AssigningStepParameters)
            .ThenInclude(p => p.Step)
            .Include(j => j.Steps)
            .ThenInclude(s => s.StepSubscriptions)
            .Include(j => j.Steps)
            .ThenInclude(s => s.Dependencies)
            .Include(j => j.Steps)
            .ThenInclude(s => s.Depending)
            .Include($"{nameof(Job.Steps)}.{nameof(IHasStepParameters.StepParameters)}")
            .Include(j => j.JobSubscriptions)
            .Include(j => j.JobTagSubscriptions)
            .Include(j => j.Schedules)
            .FirstOrDefaultAsync(j => j.JobId == request.JobId, cancellationToken);
        if (jobToRemove is not null)
        {
            context.Jobs.Remove(jobToRemove);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}