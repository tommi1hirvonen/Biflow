using Biflow.Core.Interfaces;

namespace Biflow.Ui.Api.Endpoints;

[UsedImplicitly]
public abstract class StepsReadEndpoints : IEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        var apiKeyEndpointFilterFactory = app.Services.GetRequiredService<ApiKeyEndpointFilterFactory>();
        var endpointFilter = apiKeyEndpointFilterFactory.Create([Scopes.JobsRead]);
        
        var group = app.MapGroup("/jobs/steps")
            .WithTags(Scopes.JobsRead)
            .AddEndpointFilter(endpointFilter);
        
        group.MapGet("/{stepId:guid}",
            async (ServiceDbContext dbContext, Guid stepId, CancellationToken cancellationToken) =>
            {
                var step = await dbContext.Steps
                    .AsNoTracking()
                    .Include($"{nameof(IHasStepParameters.StepParameters)}.{nameof(StepParameterBase.ExpressionParameters)}")
                    .Include($"{nameof(IHasStepParameters.StepParameters)}.{nameof(StepParameterBase.InheritFromJobParameter)}")
                    .Include(s => (s as JobStep)!.TagFilters.OrderBy(t => t.TagId))
                    .Include(s => s.Dependencies.OrderBy(d => d.DependantOnStepId))
                    .Include(s => s.DataObjects.OrderBy(d => d.ObjectId))
                    .Include(s => s.Tags.OrderBy(t => t.TagId))
                    .Include(s => s.ExecutionConditionParameters.OrderBy(p => p.ParameterId))
                    .FirstOrDefaultAsync(s => s.StepId == stepId, cancellationToken);
                if (step is null)
                {
                    throw new NotFoundException<Step>(stepId);
                }
                return Results.Ok(step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<Step>()
            .WithDescription("Get step by id")
            .WithName("GetStep");
        
        group.MapGet("/tags", async (ServiceDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var tags = await dbContext.StepTags.AsNoTracking().ToArrayAsync(cancellationToken);
                return tags;
            })
            .Produces<StepTag[]>()
            .WithDescription("Get all step tags")
            .WithName("GetStepTags");
        
        group.MapGet("/tags/{tagId:guid}", async (Guid tagId, ServiceDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var tag = await dbContext.StepTags
                              .AsNoTracking()
                              .FirstOrDefaultAsync(t => t.TagId == tagId, cancellationToken)
                          ?? throw new NotFoundException<StepTag>(tagId);
                return Results.Ok(tag);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<StepTag>()
            .WithDescription("Get step tag")
            .WithName("GetStepTag");
    }
}