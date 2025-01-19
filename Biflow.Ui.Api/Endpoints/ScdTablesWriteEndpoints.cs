using Biflow.Ui.Api.Mediator.Commands;

namespace Biflow.Ui.Api.Endpoints;

[UsedImplicitly]
public abstract class ScdTablesWriteEndpoints : IEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        var apiKeyEndpointFilterFactory = app.Services.GetRequiredService<ApiKeyEndpointFilterFactory>();
        var endpointFilter = apiKeyEndpointFilterFactory.Create([Scopes.ScdTablesWrite]);
        
        var group = app.MapGroup("/scdtables")
            .WithTags(Scopes.ScdTablesWrite)
            .AddEndpointFilter(endpointFilter);
        
        group.MapPost("", async (ScdTableDto scdTableDto, IMediator mediator,
                LinkGenerator linker, HttpContext ctx, CancellationToken cancellationToken) =>
            {
                var command = new CreateScdTableCommand(
                    ConnectionId: scdTableDto.ConnectionId,
                    ScdTableName: scdTableDto.ScdTableName,
                    SourceTableSchema: scdTableDto.SourceTableSchema,
                    SourceTableName: scdTableDto.SourceTableName,
                    TargetTableSchema: scdTableDto.TargetTableSchema,
                    TargetTableName: scdTableDto.TargetTableName,
                    StagingTableSchema: scdTableDto.StagingTableSchema,
                    StagingTableName: scdTableDto.StagingTableName,
                    PreLoadScript: scdTableDto.PreLoadScript,
                    PostLoadScript: scdTableDto.PostLoadScript,
                    FullLoad: scdTableDto.FullLoad,
                    ApplyIndexesOnCreate: scdTableDto.ApplyIndexesOnCreate,
                    SelectDistinct: scdTableDto.SelectDistinct,
                    NaturalKeyColumns: scdTableDto.NaturalKeyColumns,
                    SchemaDriftConfiguration: scdTableDto.SchemaDriftConfiguration);
                var scdTable = await mediator.SendAsync(command, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetScdTable", new { scdTableId = scdTable.ScdTableId });
                return Results.Created(url, scdTable);
            })
            .ProducesValidationProblem()
            .Produces<ScdTable>()
            .WithDescription("Create a new SCD table")
            .WithName("CreateScdTable");
        
        group.MapDelete("/{scdTableId:guid}", async (Guid scdTableId, IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new DeleteScdTableCommand(scdTableId);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithDescription("Delete an SCD table")
            .WithName("DeleteScdTable");
    }
}