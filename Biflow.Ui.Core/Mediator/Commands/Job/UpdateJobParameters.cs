namespace Biflow.Ui.Core;

public record UpdateJobParametersCommand(
    Guid JobId,
    UpdateJobParameter[] Parameters) : IRequest;

[UsedImplicitly]
internal class UpdateJobParametersCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    JobValidator jobValidator) : IRequestHandler<UpdateJobParametersCommand>
{
    public async Task Handle(UpdateJobParametersCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var job = await dbContext.Jobs
            .Include(j => j.JobParameters)
            .FirstOrDefaultAsync(j => j.JobId == request.JobId, cancellationToken)
            ?? throw new NotFoundException<Job>(request.JobId);
        
        // Remove parameters
        var parametersToRemove = job.JobParameters
            .Where(p1 => request.Parameters.All(p2 => p2.ParameterId != p1.ParameterId))
            .ToArray();
        foreach (var parameter in parametersToRemove)
        {
            job.JobParameters.Remove(parameter);
        }
        
        // Update matching parameters
        foreach (var parameter in job.JobParameters)
        {
            var updateParameter = request.Parameters
                .FirstOrDefault(p => p.ParameterId == parameter.ParameterId);
            if (updateParameter is null) continue;
            parameter.ParameterName = updateParameter.ParameterName;
            parameter.ParameterValue = updateParameter.ParameterValue ?? new ParameterValue();
            parameter.UseExpression = updateParameter.UseExpression;
            parameter.Expression.Expression = updateParameter.Expression;
            parameter.EnsureDataAnnotationsValidated();
        }

        // Add parameters
        var parametersToAdd = request.Parameters
            .Where(p1 => job.JobParameters.All(p2 => p2.ParameterId != p1.ParameterId));
        foreach (var createParameter in parametersToAdd)
        {
            var jobParameter = new JobParameter
            {
                JobId = request.JobId,
                ParameterName = createParameter.ParameterName,
                ParameterValue = createParameter.ParameterValue ?? new ParameterValue(),
                UseExpression = createParameter.UseExpression,
                Expression = new EvaluationExpression { Expression = createParameter.Expression }
            };
            jobParameter.EnsureDataAnnotationsValidated();
            job.JobParameters.Add(jobParameter);
        }
        
        await jobValidator.EnsureValidatedAsync(job, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}