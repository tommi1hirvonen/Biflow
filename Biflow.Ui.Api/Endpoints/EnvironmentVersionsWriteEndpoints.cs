using System.Collections.Concurrent;
using Biflow.Ui.Core.Projection;
using Microsoft.AspNetCore.Mvc;

namespace Biflow.Ui.Api.Endpoints;

[UsedImplicitly]
public abstract class EnvironmentVersionsWriteEndpoints : IEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        var apiKeyEndpointFilterFactory = app.Services.GetRequiredService<ApiKeyEndpointFilterFactory>();
        var endpointFilter = apiKeyEndpointFilterFactory.Create([Scopes.EnvironmentVersionsWrite]);
        
        var group = app.MapGroup("/environmentversions")
            .WithTags(Scopes.EnvironmentVersionsWrite)
            .AddEndpointFilter(endpointFilter);
        
        group.MapPost("",
            async ([FromBody] EnvironmentVersionDto model, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new CreateVersionCommand(model.Description);
                var version = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(version);
            })
            .Produces<VersionProjection>()
            .WithDescription("Create a new environment version from the current environment state.")
            .WithName("CreateEnvironmentVersion");
        
        group.MapGet("/revert/status/{id:guid}",
            (Guid id, ConcurrentDictionary<Guid, VersionRevertStatus> statuses) =>
            {
                if (!statuses.TryGetValue(id, out var status))
                {
                    return Results.Problem(
                        detail: $"No version revert job found with id {id}",
                        statusCode: StatusCodes.Status404NotFound);
                }
                return Results.Ok(new VersionRevertResponse(id, status));
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<VersionRevertResponse>()
            .WithName("GetRevertStatus");

        group.MapPost("/revert/{versionId:int}",
            (int versionId,
                IMediator mediator,
                HttpContext httpContext,
                LinkGenerator linker,
                VersionRevertService versionRevertService,
                ConcurrentDictionary<Guid, VersionRevertStatus> statuses) =>
            {
                var command = new RevertVersionByIdCommand(versionId);
                var handler = mediator.GetRequestHandler<RevertVersionByIdCommand>();
                var job = new VersionRevertJob(token => handler.Handle(command, token));
                if (!versionRevertService.TryEnqueue(job))
                {
                    return Results.Problem(
                        detail: "Previous version revert process is still running.",
                        statusCode: StatusCodes.Status409Conflict);
                }
                statuses[job.Id] = VersionRevertStatus.Queued;
                var url = linker.GetUriByName(httpContext, "GetRevertStatus", new { id = job.Id }); 
                return Results.Accepted(url, new VersionRevertResponse(job.Id, VersionRevertStatus.Queued));
            })
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<VersionRevertResponse>(StatusCodes.Status202Accepted)
            .WithDescription("Revert the environment to the version defined by the version id query parameter")
            .WithName("RevertEnvironmentVersionById");
        
        group.MapPost("/revert",
            async (IMediator mediator,
                HttpContext httpContext,
                LinkGenerator linker,
                VersionRevertService versionRevertService,
                ConcurrentDictionary<Guid, VersionRevertStatus> statuses) =>
            {
                using var reader = new StreamReader(httpContext.Request.Body);
                var json = await reader.ReadToEndAsync(); 
                // Assume referencesPreserved = true, because only then is reverting of the snapshot supported.
                var snapshot = EnvironmentSnapshot.FromJson(json, referencesPreserved: true);
                ArgumentNullException.ThrowIfNull(snapshot);
                var command = new RevertVersionCommand(snapshot);
                var handler = mediator.GetRequestHandler<RevertVersionCommand>();
                var job = new VersionRevertJob(token => handler.Handle(command, token));
                if (!versionRevertService.TryEnqueue(job))
                {
                    return Results.Problem(
                        detail: "Previous version revert process is still running.",
                        statusCode: StatusCodes.Status409Conflict);
                }
                statuses[job.Id] = VersionRevertStatus.Queued;
                var url = linker.GetUriByName(httpContext, "GetRevertStatus", new { id = job.Id }); 
                return Results.Accepted(url, new VersionRevertResponse(job.Id, VersionRevertStatus.Queued));
            })
            .Accepts<string>("application/json")
            .ProducesProblem(StatusCodes.Status409Conflict)
            .Produces<VersionRevertResponse>(StatusCodes.Status202Accepted)
            .WithDescription("Revert the environment to the snapshot provided in the request body." +
                             " The snapshot must be provided in the format where object references have been preserved.")
            .WithName("RevertEnvironmentVersion");
    }
}