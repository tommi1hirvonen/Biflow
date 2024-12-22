using Biflow.Core.Constants;
using Biflow.Ui.Core;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Api.Endpoints;

[UsedImplicitly]
public abstract class DataTablesReadEndpoints : IEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        var apiKeyEndpointFilterFactory = app.Services.GetRequiredService<ApiKeyEndpointFilterFactory>();
        var endpointFilter = apiKeyEndpointFilterFactory.Create([Scopes.DataTablesRead]);
        
        var group = app.MapGroup("/datatables")
            .WithTags(Scopes.DataTablesRead)
            .AddEndpointFilter(endpointFilter);

        group.MapGet("", async (ServiceDbContext dbContext, CancellationToken cancellationToken) =>
                await dbContext.MasterDataTables
                    .AsNoTracking()
                    .Include(t => t.Lookups)
                    .OrderBy(t => t.Category!.CategoryName)
                    .ThenBy(t => t.DataTableName)
                    .ToArrayAsync(cancellationToken)
            )
            .WithDescription("Get all data tables")
            .WithName("GetDataTables");
        
        group.MapGet("/{dataTableId:guid}",
                async (ServiceDbContext dbContext, Guid dataTableId, CancellationToken cancellationToken) =>
                {
                    var scdTable = await dbContext.MasterDataTables
                        .AsNoTracking()
                        .Include(t => t.Lookups)
                        .FirstOrDefaultAsync(t => t.DataTableId == dataTableId, cancellationToken);
                    return scdTable is null ? Results.NotFound() : Results.Ok(scdTable);
                })
            .WithDescription("Get data table by id")
            .WithName("GetDataTable");
        
        group.MapGet("/categories", async (ServiceDbContext dbContext, CancellationToken cancellationToken) =>
                await dbContext.MasterDataTableCategories
                    .AsNoTracking()
                    .OrderBy(c => c.CategoryName)
                    .ToArrayAsync(cancellationToken)
            )
            .WithDescription("Get all data table categories")
            .WithName("GetDataTableCategories");
    }
}