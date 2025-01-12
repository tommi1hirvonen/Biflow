using Biflow.Ui.Api.Mediator.Commands;

namespace Biflow.Ui.Api.Endpoints;

[UsedImplicitly]
public abstract class StepsWriteEndpoints : IEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        var apiKeyEndpointFilterFactory = app.Services.GetRequiredService<ApiKeyEndpointFilterFactory>();
        var endpointFilter = apiKeyEndpointFilterFactory.Create([Scopes.JobsWrite]);
        
        var group = app.MapGroup("/jobs/steps")
            .WithTags(Scopes.JobsWrite)
            .AddEndpointFilter(endpointFilter);
        
        group.MapPost("/{stepId:guid}/tags/{tagId:guid}",
            async (Guid stepId, Guid tagId, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new CreateStepTagRelationCommand(stepId, tagId);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithDescription("Create a step tag relation")
            .WithName("CreateStepTagRelation");
        
        group.MapDelete("/{stepId:guid}",
            async (Guid stepId, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new DeleteStepsCommand(stepId);
                var steps = await mediator.SendAsync(command, cancellationToken);
                if (steps.Length == 0)
                {
                    throw new NotFoundException<Step>(stepId);
                }
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithDescription("Delete a step")
            .WithName("DeleteStep");
        
        group.MapDelete("/{stepId:guid}/tags/{tagId:guid}",
            async (Guid stepId, Guid tagId, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new DeleteStepTagRelationCommand(stepId, tagId);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithDescription("Delete a step tag relation")
            .WithName("DeleteStepTagRelation");
        
        group.MapDelete("/tags/{tagId:guid}",
            async (Guid tagId, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new DeleteStepTagCommand(tagId);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithDescription("Delete a step tag")
            .WithName("DeleteStepTag");
    }
}