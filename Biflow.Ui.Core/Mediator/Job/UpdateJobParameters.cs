using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Core;

public record UpdateJobParametersCommand(Job Job) : IRequest;

internal class UpdateJobParametersCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateJobParametersCommand>
{
    public async Task Handle(UpdateJobParametersCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var job = await context.Jobs
            .Include(j => j.JobParameters)
            .FirstOrDefaultAsync(j => j.JobId == request.Job.JobId, cancellationToken);
        if (job is null)
        {
            return;
        }
        context.MergeCollections(job.JobParameters, request.Job.JobParameters, p => p.ParameterId);
        await context.SaveChangesAsync(cancellationToken);
    }
}