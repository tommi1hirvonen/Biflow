using Biflow.Core.Constants;
using Biflow.Ui.Core;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Api.Endpoints;

[UsedImplicitly]
public class ScdTablesReadEndpoints : IEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        var apiKeyEndpointFilterFactory = app.Services.GetRequiredService<ApiKeyEndpointFilterFactory>();
        var endpointFilter = apiKeyEndpointFilterFactory.Create([Scopes.ScdTablesRead]);
        
        var group = app.MapGroup("/scdtables")
            .WithTags("ScdTables.Read")
            .AddEndpointFilter(endpointFilter);

        group.MapGet("", async (ServiceDbContext dbContext, CancellationToken cancellationToken) =>
            await dbContext.ScdTables.ToArrayAsync(cancellationToken)
            )
            .WithDescription("Get all SCD tables")
            .WithName("GetScdTables");
        
        group.MapGet("/{scdTableId:guid}",
            async (ServiceDbContext dbContext, Guid scdTableId, CancellationToken cancellationToken) =>
            {
                var scdTable = await dbContext.ScdTables
                    .FirstOrDefaultAsync(t => t.ScdTableId == scdTableId, cancellationToken);
                return scdTable is null ? Results.NotFound() : Results.Ok(scdTable);
            })
            .WithDescription("Get SCD table by id")
            .WithName("GetScdTable");
    }
}