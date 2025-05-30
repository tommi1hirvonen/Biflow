using Biflow.Ui.Api.Mediator.Commands;
using Microsoft.AspNetCore.Mvc;

namespace Biflow.Ui.Api.Endpoints;

[UsedImplicitly]
public abstract class JobsWriteEndpoints : IEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        var apiKeyEndpointFilterFactory = app.Services.GetRequiredService<ApiKeyEndpointFilterFactory>();
        var endpointFilter = apiKeyEndpointFilterFactory.Create([Scopes.JobsWrite]);
        
        var group = app.MapGroup("/jobs")
            .WithTags(Scopes.JobsWrite)
            .AddEndpointFilter(endpointFilter);
        
        group.MapPost("",
            async ([FromBody] CreateJob request, IMediator mediator, LinkGenerator linker, HttpContext ctx, CancellationToken cancellationToken) =>
            {
                var command = new CreateJobCommand(
                    JobName: request.JobName,
                    JobDescription: request.JobDescription,
                    ExecutionMode: request.ExecutionMode,
                    StopOnFirstError: request.StopOnFirstError,
                    MaxParallelSteps: request.MaxParallelSteps,
                    OvertimeNotificationLimitMinutes: request.OvertimeNotificationLimitMinutes,
                    TimeoutMinutes: request.TimeoutMinutes,
                    IsEnabled: request.IsEnabled,
                    IsPinned: request.IsPinned,
                    JobTagIds: request.JobTagIds);
                var job = await mediator.SendAsync(command, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetJob", new { jobId = job.JobId });
                return Results.Created(url, job);
            })
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem()
            .Produces<Job>(StatusCodes.Status201Created)
            .WithSummary("Create job")
            .WithDescription("Create a new job")
            .WithName("CreateJob");
        
        group.MapPut("/{jobId:guid}",
            async (Guid jobId, [FromBody] UpdateJob request, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new UpdateJobCommand(
                    JobId: jobId,
                    JobName: request.JobName,
                    JobDescription: request.JobDescription,
                    ExecutionMode: request.ExecutionMode,
                    StopOnFirstError: request.StopOnFirstError,
                    OvertimeNotificationLimitMinutes: request.OvertimeNotificationLimitMinutes,
                    TimeoutMinutes: request.TimeoutMinutes,
                    JobTagIds: request.JobTagIds);
                var job = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(job);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<Job>()
            .WithSummary("Update job by id")
            .WithDescription("Update an existing job with the given id")
            .WithName("UpdateJob");
        
        group.MapDelete("/{jobId:guid}", async (Guid jobId, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new DeleteJobCommand(jobId);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Delete job by id")
            .WithDescription("Delete a job with the given id")
            .WithName("DeleteJob");
        
        group.MapPatch("/{jobId:guid}/parameters",
            async ([FromRoute] Guid jobId, [FromBody] JobParameterDto[] parameters,
                IMediator mediator, CancellationToken cancellationToken) =>
            {
                var updateParameters = parameters
                    .Select(x => new UpdateJobParameter(
                        x.ParameterId,
                        x.ParameterName,
                        x.ParameterValue,
                        x.UseExpression,
                        x.Expression))
                    .ToArray();
                var command = new UpdateJobParametersCommand(jobId, updateParameters);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Update job parameters")
            .WithDescription("Update job parameters for an existing job. " +
                             "Parameters with a matching parameter id will be updated. " +
                             "Parameters in the request body with no matching id will be created/added. " +
                             "Existing parameters with no matching parameter id in the request body will be removed. " +
                             "NOTE: Step parameters may reference job parameters. Before removing job parameters, " +
                             "make sure there are no step parameters that reference the job parameter.")
            .WithName("UpdateJobParameters");

        group.MapPatch("/{jobId:guid}/concurrencies",
            async ([FromRoute] Guid jobId, [FromBody] JobConcurrencies concurrencies,
                IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dictionary = concurrencies.StepTypeConcurrencies
                    .ToDictionary(key => key.StepType, value => value.MaxParallelSteps);
                var command = new UpdateJobConcurrenciesCommand(jobId, concurrencies.MaxParallelSteps, dictionary);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Update job concurrencies")
            .WithDescription("Update job concurrencies for an existing job")
            .WithName("UpdateJobConcurrencies");
        
        group.MapPatch("/{jobId:guid}/pinned",
            async (Guid jobId, PinnedDto pinned, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new ToggleJobPinnedCommand(jobId, pinned.IsPinned);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Toggle whether job is pinned or not")
            .WithDescription("Toggle whether the job is pinned or not")
            .WithName("ToggleJobPinned");
        
        group.MapPatch("/{jobId:guid}/state",
            async (Guid jobId, StateDto state, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new ToggleJobEnabledCommand(jobId, state.IsEnabled);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Toggle the state of a job")
            .WithDescription("Toggle the state of an existing job (enabled/disabled)")
            .WithName("ToggleJobEnabled");
        
        group.MapPost("/{jobId:guid}/tags/{tagId:guid}",
            async (Guid jobId, Guid tagId, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new CreateJobTagRelationCommand(jobId, tagId);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Create job tag relation")
            .WithDescription("Create a job tag relation")
            .WithName("CreateJobTagRelation");
        
        group.MapDelete("/{jobId:guid}/tags/{tagId:guid}",
            async (Guid jobId, Guid tagId, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new DeleteJobTagRelationCommand(jobId, tagId);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Delete job tag relation")
            .WithDescription("Delete a job tag relation")
            .WithName("DeleteJobTagRelation");
        
        group.MapPost("/tags",
            async ([FromBody] TagDto tagDto, IMediator mediator, LinkGenerator linker, HttpContext ctx, CancellationToken cancellationToken) =>
            {
                var command = new CreateJobTagCommand(
                    TagName: tagDto.TagName,
                    Color: tagDto.Color,
                    SortOrder: tagDto.SortOrder);
                var tag = await mediator.SendAsync(command, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetJobTag", new { tagId = tag.TagId });
                return Results.Created(url, tag);
            })
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem()
            .Produces<JobTag>(StatusCodes.Status201Created)
            .WithSummary("Create job tag")
            .WithDescription("Create a new job tag")
            .WithName("CreateJobTag");

        group.MapPut("/tags/{tagId:guid}",
            async (Guid tagId, [FromBody] TagDto tagDto, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new UpdateJobTagCommand(
                    TagId: tagId,
                    TagName: tagDto.TagName,
                    Color: tagDto.Color,
                    SortOrder: tagDto.SortOrder);
                var tag = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(tag);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<JobTag>()
            .WithSummary("Update job tag")
            .WithDescription("Update an existing job tag")
            .WithName("UpdateJobTag");
        
        group.MapDelete("/tags/{tagId:guid}", async (Guid tagId, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new DeleteJobTagCommand(tagId);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Delete job tag")
            .WithDescription("Delete a job tag")
            .WithName("DeleteJobTag");
        
        var dataObjectsGroup = app.MapGroup("/dataobjects")
            .WithTags(Scopes.JobsWrite)
            .AddEndpointFilter(endpointFilter);
        
        dataObjectsGroup.MapPost("",
            async ([FromBody] DataObjectDto dataObjectDto,
                IMediator mediator,
                LinkGenerator linker,
                HttpContext ctx,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateDataObjectCommand(dataObjectDto.ObjectUri, dataObjectDto.MaxConcurrentWrites);
                var dataObject = await mediator.SendAsync(command, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetDataObject", new { dataObjectId = dataObject.ObjectId });
                return Results.Created(url, dataObject);
            })
            .ProducesValidationProblem()
            .Produces<DataObject>(StatusCodes.Status201Created)
            .WithSummary("Create data object")
            .WithDescription("Create a new data object")
            .WithName("CreateDataObject");
        
        dataObjectsGroup.MapPut("/{dataObjectId:guid}",
            async ([FromRoute] Guid dataObjectId,
                [FromBody] DataObjectDto dataObjectDto,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateDataObjectCommand(
                    dataObjectId, dataObjectDto.ObjectUri, dataObjectDto.MaxConcurrentWrites);
                var dataObject = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(dataObject);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<DataObject>()
            .WithSummary("Update data object")
            .WithDescription("Update an existing data object")
            .WithName("UpdateDataObject");
        
        dataObjectsGroup.MapDelete("/{dataObjectId:guid}",
            async (Guid dataObjectId, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new DeleteDataObjectCommand(dataObjectId);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Delete data object")
            .WithDescription("Delete a data object")
            .WithName("DeleteDataObject");
    }
}