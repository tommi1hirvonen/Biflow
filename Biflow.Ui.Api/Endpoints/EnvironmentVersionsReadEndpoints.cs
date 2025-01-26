using Biflow.Ui.Core.Projection;
using Microsoft.AspNetCore.Mvc;

namespace Biflow.Ui.Api.Endpoints;

[UsedImplicitly]
public abstract class EnvironmentVersionsReadEndpoints : IEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        var apiKeyEndpointFilterFactory = app.Services.GetRequiredService<ApiKeyEndpointFilterFactory>();
        var endpointFilter = apiKeyEndpointFilterFactory.Create([Scopes.EnvironmentVersionsRead]);
        
        var group = app.MapGroup("/environmentversions")
            .WithTags(Scopes.EnvironmentVersionsRead)
            .AddEndpointFilter(endpointFilter);
        
        group.MapGet("",
            async (ServiceDbContext dbContext,
                    CancellationToken cancellationToken,
                    [FromQuery] int startVersionId = 0,
                    [FromQuery] int limit = 100) =>
                await dbContext.EnvironmentVersions
                    .AsNoTracking()
                    .OrderBy(v => v.VersionId)
                    .Where(v => v.VersionId > startVersionId)
                    .Take(limit)
                    .Select(v => new VersionProjection(v.VersionId, v.Description, v.CreatedOn, v.CreatedBy))
                    .ToArrayAsync(cancellationToken)
            )
            .Produces<VersionProjection[]>()
            .WithSummary("Get all environment versions")
            .WithDescription("Get all environment versions. " +
                             "Use the query parameters startVersionId and limit for pagination.")
            .WithName("GetEnvironmentVersions");
        
        group.MapGet("/{versionId:int}",
            async (ServiceDbContext dbContext, int versionId, CancellationToken cancellationToken) =>
            {
                var version = await dbContext.EnvironmentVersions
                    .Where(v => v.VersionId == versionId)
                    .Select(v => new VersionProjection(v.VersionId, v.Description, v.CreatedOn, v.CreatedBy))
                    .FirstOrDefaultAsync(cancellationToken);
                if (version is null)
                {
                    throw new NotFoundException<EnvironmentVersion>(versionId);
                }
                return Results.Ok(version);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<VersionProjection>()
            .WithSummary("Get environment version by id")
            .WithDescription("Get environment version by id")
            .WithName("GetEnvironmentVersion");
        
        group.MapGet("/{versionId:int}/snapshot",
            async (HttpContext httpContext,
                ServiceDbContext dbContext,
                int versionId,
                CancellationToken cancellationToken,
                [FromQuery] bool referencesPreserved = false) =>
            {
                var version = await dbContext.EnvironmentVersions
                    .FirstOrDefaultAsync(v => v.VersionId == versionId, cancellationToken);
                if (version is null)
                {
                    throw new NotFoundException<EnvironmentVersion>(versionId);
                }
                httpContext.Response.ContentType = "application/json";
                return referencesPreserved
                    ? version.SnapshotWithReferencesPreserved
                    : version.Snapshot; 
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<EnvironmentSnapshot>()
            .WithSummary("Get environment snapshot by version id")
            .WithDescription("Get environment version snapshot. " +
                             "Use the referencesPreserved query parameter to return a variant of the snapshot " +
                             "that can be used for reverting to a version.")
            .WithName("GetEnvironmentVersionSnapshot");
    }
}