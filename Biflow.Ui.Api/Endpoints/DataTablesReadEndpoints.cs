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
            .Produces<MasterDataTable[]>()
            .WithDescription("Get all data tables")
            .WithName("GetDataTables");
        
        group.MapGet("/{dataTableId:guid}",
            async (ServiceDbContext dbContext, Guid dataTableId, CancellationToken cancellationToken) =>
            {
                var scdTable = await dbContext.MasterDataTables
                    .AsNoTracking()
                    .Include(t => t.Lookups)
                    .FirstOrDefaultAsync(t => t.DataTableId == dataTableId, cancellationToken);
                if (scdTable is null)
                {
                    throw new NotFoundException<MasterDataTable>(dataTableId);
                }
                return Results.Ok(scdTable);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<MasterDataTable>()
            .WithDescription("Get data table by id")
            .WithName("GetDataTable");
        
        group.MapGet("/categories", async (ServiceDbContext dbContext, CancellationToken cancellationToken) =>
                await dbContext.MasterDataTableCategories
                    .AsNoTracking()
                    .OrderBy(c => c.CategoryName)
                    .ToArrayAsync(cancellationToken)
            )
            .Produces<MasterDataTableCategory[]>()
            .WithDescription("Get all data table categories")
            .WithName("GetDataTableCategories");
        
        group.MapGet("/categories/{categoryId:guid}",
            async (ServiceDbContext dbContext, Guid categoryId, CancellationToken cancellationToken) =>
            {
                var category = await dbContext.MasterDataTableCategories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.CategoryId == categoryId, cancellationToken)
                    ?? throw new NotFoundException<MasterDataTableCategory>(categoryId);
                return Results.Ok(category);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<MasterDataTableCategory>()
            .WithDescription("Get data table category by id")
            .WithName("GetDataTableCategory");
    }
}