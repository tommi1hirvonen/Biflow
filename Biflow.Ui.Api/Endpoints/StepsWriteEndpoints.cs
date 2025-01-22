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

        group.MapPatch("/{stepId:guid}/state", async (Guid stepId, StateDto stateDto, IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new ToggleStepEnabledCommand(stepId, stateDto.IsEnabled);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithDescription("Toggle the state of an existing step")
            .WithName("ToggleStepEnabled");
        
        group.MapPut("/{stepId:guid}/dependencies", async (Guid stepId, DependencyDto[] dependencyDtos,
            IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dictionary = dependencyDtos.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var command = new UpdateStepDependenciesCommand(stepId, dictionary);
                var dependencies = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(dependencies);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<Dependency[]>()
            .WithDescription("Update the dependencies of an existing step")
            .WithName("UpdateStepDependencies");
        
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

        group.MapPost("/tags", async (TagDto tagDto, IMediator mediator,
            LinkGenerator linker, HttpContext ctx, CancellationToken cancellationToken) =>
            {
                var command = new CreateStepTagCommand(tagDto.TagName, tagDto.Color, tagDto.SortOrder);
                var tag = await mediator.SendAsync(command, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetStepTag", new { tagId = tag.TagId });
                return Results.Created(url, tag);
            })
            .ProducesValidationProblem()
            .Produces<StepTag>(StatusCodes.Status201Created)
            .WithDescription("Create a new step tag")
            .WithName("CreateStepTag");
        
        group.MapPut("/tags/{tagId:guid}",
                async (Guid tagId, TagDto tagDto, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var command = new UpdateStepTagCommand(
                        TagId: tagId,
                        TagName: tagDto.TagName,
                        Color: tagDto.Color,
                        SortOrder: tagDto.SortOrder);
                    var tag = await mediator.SendAsync(command, cancellationToken);
                    return Results.Ok(tag);
                })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<StepTag>()
            .WithDescription("Update an existing step tag")
            .WithName("UpdateStepTag");
        
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