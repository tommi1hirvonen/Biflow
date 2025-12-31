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
        
        group.MapGet("/propertytranslationsets", async (ServiceDbContext dbContext, CancellationToken cancellationToken) =>
                await dbContext.PropertyTranslationSets
                    .AsNoTracking()
                    .Include(s => s.PropertyTranslations)
                    .OrderBy(s => s.PropertyTranslationSetName)
                    .ToArrayAsync(cancellationToken)
            )
            .Produces<PropertyTranslationSet[]>()
            .WithSummary("Get all property translation sets")
            .WithDescription("Get all property translation sets")
            .WithName("GetPropertyTranslationSets");

        group.MapGet("/propertytranslationsets/{propertyTranslationSetId:guid}",
            async (ServiceDbContext dbContext, Guid propertyTranslationSetId, CancellationToken cancellationToken) =>
            {
                var set = await dbContext.PropertyTranslationSets
                    .AsNoTracking()
                    .Include(s => s.PropertyTranslations)
                    .FirstOrDefaultAsync(s => s.PropertyTranslationSetId == propertyTranslationSetId, cancellationToken);
                if (set is null)
                {
                    throw new NotFoundException<PropertyTranslationSet>(propertyTranslationSetId);
                }
                return Results.Ok(set);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<PropertyTranslationSet>()
            .WithSummary("Get property translation set by id")
            .WithDescription("Get property translation set by id")
            .WithName("GetPropertyTranslationSet");

        group.MapGet("/propertytranslations", async (ServiceDbContext dbContext, CancellationToken cancellationToken) =>
                await dbContext.PropertyTranslations
                    .AsNoTracking()
                    .OrderBy(t => t.PropertyTranslationName)
                    .ToArrayAsync(cancellationToken)
            )
            .Produces<PropertyTranslation[]>()
            .WithSummary("Get all property translations")
            .WithDescription("Get all property translations")
            .WithName("GetPropertyTranslations");

        group.MapGet("/propertytranslations/{propertyTranslationId:guid}",
            async (ServiceDbContext dbContext, Guid propertyTranslationId, CancellationToken cancellationToken) =>
            {
                var translation = await dbContext.PropertyTranslations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.PropertyTranslationId == propertyTranslationId, cancellationToken)
                    ?? throw new NotFoundException<PropertyTranslation>(propertyTranslationId);
                return Results.Ok(translation);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<PropertyTranslation>()
            .WithSummary("Get property translation by id")
            .WithDescription("Get property translation by id")
            .WithName("GetPropertyTranslation");
    }
}