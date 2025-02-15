using System.Collections.Concurrent;
using Biflow.Ui.Core.Projection;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

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
            .WithSummary("Create a new environment version")
            .WithDescription("Create a new environment version and snapshot from the current environment state.")
            .WithName("CreateEnvironmentVersion");
        
        group.MapGet("/revert/status/{id:guid}",
            (Guid id, VersionRevertJobDictionary statuses) =>
            {
                if (!statuses.TryGetValue(id, out var state))
                {
                    return Results.Problem(
                        detail: $"No version revert job found with id {id}",
                        statusCode: StatusCodes.Status404NotFound);
                }
                var integrations = state.NewIntegrations
                    .Select(x => new VersionRevertResponseIntegration(x.Type, x.Name))
                    .ToArray();
                return Results.Ok(new VersionRevertResponse(id, state.Status, integrations));
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<VersionRevertResponse>()
            .WithSummary("Get revert process status by revert job id")
            .WithDescription("Get environment version revert process status by revert job id. " +
                             "The newIntegrations array in the response JSON lists integration entities that were " +
                             "added/created as new integrations. The property values of these integration entities " +
                             "should be checked after the revert as they may need to be filled in or corrected.")
            .WithName("GetRevertStatus");

        group.MapPost("/revert/{versionId:int}",
            (int versionId,
                [FromQuery]
                [SwaggerParameter("Whether to retain previous integration properties. " +
                                  "This should normally be set to true if transferring snapshots between " +
                                  "environments (e. g. from test to prod) where integration property values for " +
                                  "the same entity may be different (e. g. connection strings or resource names).")]
                bool retainIntegrationProperties,
                IMediator mediator,
                HttpContext httpContext,
                LinkGenerator linker,
                VersionRevertService versionRevertService,
                VersionRevertJobDictionary statuses) =>
            {
                var command = new RevertVersionByIdCommand(versionId, retainIntegrationProperties);
                var handler = mediator.GetRequestHandler<RevertVersionByIdCommand, RevertVersionResponse>();
                var job = new VersionRevertJob(token => handler.Handle(command, token));
                if (!versionRevertService.TryEnqueue(job))
                {
                    return Results.Problem(
                        detail: "Previous version revert process is still running.",
                        statusCode: StatusCodes.Status409Conflict);
                }
                statuses[job.Id] = new VersionRevertJobState(VersionRevertJobStatus.Queued, []);
                var url = linker.GetUriByName(httpContext, "GetRevertStatus", new { id = job.Id }); 
                return Results.Accepted(url, new VersionRevertResponse(job.Id, VersionRevertJobStatus.Queued, []));
            })
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<VersionRevertResponse>(StatusCodes.Status202Accepted)
            .WithSummary("Revert the environment to the specified version")
            .WithDescription("Revert the environment to the version defined by the version id route parameter. " +
                             "The process is asynchronous and a response is returned immediately. " +
                             "Query the status endpoint to get the process status with the returned id.")
            .WithName("RevertEnvironmentVersionById");
        
        group.MapPost("/revert", async (
                [FromQuery]
                [SwaggerParameter("Whether to retain previous integration properties. " +
                                  "This should normally be set to true if transferring snapshots between " +
                                  "environments (e. g. from test to prod) where integration property values for " +
                                  "the same entity may be different (e. g. connection strings or resource names).")]
                bool retainIntegrationProperties,
                IMediator mediator,
                HttpContext httpContext,
                LinkGenerator linker,
                VersionRevertService versionRevertService,
                VersionRevertJobDictionary statuses) =>
            {
                using var reader = new StreamReader(httpContext.Request.Body);
                var json = await reader.ReadToEndAsync(); 
                // Assume referencesPreserved = true, because only then is reverting of the snapshot supported.
                var snapshot = EnvironmentSnapshot.FromJson(json, referencesPreserved: true);
                ArgumentNullException.ThrowIfNull(snapshot);
                var command = new RevertVersionCommand(snapshot, retainIntegrationProperties);
                var handler = mediator.GetRequestHandler<RevertVersionCommand, RevertVersionResponse>();
                var job = new VersionRevertJob(token => handler.Handle(command, token));
                if (!versionRevertService.TryEnqueue(job))
                {
                    return Results.Problem(
                        detail: "Previous version revert process is still running.",
                        statusCode: StatusCodes.Status409Conflict);
                }
                statuses[job.Id] = new VersionRevertJobState(VersionRevertJobStatus.Queued, []);
                var url = linker.GetUriByName(httpContext, "GetRevertStatus", new { id = job.Id }); 
                return Results.Accepted(url, new VersionRevertResponse(job.Id, VersionRevertJobStatus.Queued, []));
            })
            .Accepts<string>("application/json")
            .ProducesProblem(StatusCodes.Status409Conflict)
            .Produces<VersionRevertResponse>(StatusCodes.Status202Accepted)
            .WithSummary("Revert the environment to a provided snapshot")
            .WithDescription("Revert the environment to the snapshot provided in the request body. " +
                             "The snapshot must be provided in the format where object references have been preserved." +
                             "The process is asynchronous and a response is returned immediately. " +
                             "Query the status endpoint to get the process status with the returned id.")
            .WithName("RevertEnvironmentVersion");
    }
}