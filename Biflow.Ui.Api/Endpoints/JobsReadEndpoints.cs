using Biflow.Core.Constants;
using Biflow.Core.Entities;
using Biflow.Core.Interfaces;
using Biflow.Ui.Core;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Api.Endpoints;

[UsedImplicitly]
public abstract class JobsReadEndpoints : IEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        var apiKeyEndpointFilterFactory = app.Services.GetRequiredService<ApiKeyEndpointFilterFactory>();
        var endpointFilter = apiKeyEndpointFilterFactory.Create([Scopes.JobsRead]);
        
        var group = app.MapGroup("/jobs")
            .WithTags("Jobs.Read")
            .AddEndpointFilter(endpointFilter);
        
        group.MapGet("", async (ServiceDbContext dbContext, CancellationToken cancellationToken) =>
            await dbContext.Jobs
                .AsNoTracking()
                .OrderBy(j => !j.IsPinned)
                .ThenBy(j => j.JobName)
                .ToArrayAsync(cancellationToken)
            )
            .WithDescription("Get all jobs. Collection properties" +
                             "(job parameters, concurrences, tags, steps, schedules) " +
                             "are not loaded and will be empty.")
            .WithName("GetJobs");

        group.MapGet("/{jobId:guid}",
            async (ServiceDbContext dbContext, Guid jobId, CancellationToken cancellationToken) =>
        {
            var job = await dbContext.Jobs
                .Include(j => j.JobParameters)
                .Include(j => j.JobConcurrencies)
                .Include(j => j.Tags)
                .Include(j => j.Steps)
                .Include(j => j.Schedules)
                .FirstOrDefaultAsync(j => j.JobId == jobId, cancellationToken);
            return job is null ? Results.NotFound() : Results.Ok(job);
        }).WithDescription("Get job by id. Collection properties of steps and schedules are not loaded and will be empty.")
            .WithName("GetJob");
        
        group.MapGet("/steps/{stepId:guid}",
            async (ServiceDbContext dbContext, Guid stepId, CancellationToken cancellationToken) =>
        {
            var step = await dbContext.Steps
                .Include($"{nameof(IHasStepParameters.StepParameters)}.{nameof(StepParameterBase.ExpressionParameters)}")
                .Include($"{nameof(IHasStepParameters.StepParameters)}.{nameof(StepParameterBase.InheritFromJobParameter)}")
                .Include(s => (s as JobStep)!.TagFilters.OrderBy(t => t.TagId))
                .Include(s => s.Dependencies.OrderBy(d => d.DependantOnStepId))
                .Include(s => s.DataObjects.OrderBy(d => d.ObjectId))
                .Include(s => s.Tags.OrderBy(t => t.TagId))
                .Include(s => s.ExecutionConditionParameters.OrderBy(p => p.ParameterId))
                .FirstOrDefaultAsync(s => s.StepId == stepId, cancellationToken);
            return step is null ? Results.NotFound() : Results.Ok(step);
        }).WithDescription("Get step by id")
            .WithName("GetStep");
    }
}