namespace Biflow.Ui.Api.Endpoints;

[UsedImplicitly]
public abstract class ScdTablesReadEndpoints : IEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        var apiKeyEndpointFilterFactory = app.Services.GetRequiredService<ApiKeyEndpointFilterFactory>();
        var endpointFilter = apiKeyEndpointFilterFactory.Create([Scopes.ScdTablesRead]);
        
        var group = app.MapGroup("/scdtables")
            .WithTags(Scopes.ScdTablesRead)
            .AddEndpointFilter(endpointFilter);

        group.MapGet("", async (ServiceDbContext dbContext, CancellationToken cancellationToken) =>
            await dbContext.ScdTables
                .AsNoTracking()
                .OrderBy(t => t.ScdTableName)
                .ToArrayAsync(cancellationToken)
            )
            .Produces<ScdTable[]>()
            .WithDescription("Get all SCD tables")
            .WithName("GetScdTables");
        
        group.MapGet("/{scdTableId:guid}",
            async (ServiceDbContext dbContext, Guid scdTableId, CancellationToken cancellationToken) =>
            {
                var scdTable = await dbContext.ScdTables
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.ScdTableId == scdTableId, cancellationToken);
                if (scdTable is null)
                {
                    throw new NotFoundException<ScdTable>(scdTableId);
                }
                return Results.Ok(scdTable);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<ScdTable>()
            .WithDescription("Get SCD table by id")
            .WithName("GetScdTable");
    }
}