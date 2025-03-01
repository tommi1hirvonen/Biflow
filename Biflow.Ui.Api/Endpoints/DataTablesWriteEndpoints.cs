using Biflow.Ui.Api.Mediator.Commands;

namespace Biflow.Ui.Api.Endpoints;

[UsedImplicitly]
public class DataTablesWriteEndpoints : IEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        var apiKeyEndpointFilterFactory = app.Services.GetRequiredService<ApiKeyEndpointFilterFactory>();
        var endpointFilter = apiKeyEndpointFilterFactory.Create([Scopes.DataTablesWrite]);
        
        var group = app.MapGroup("/datatables")
            .WithTags(Scopes.DataTablesWrite)
            .AddEndpointFilter(endpointFilter);
        
        group.MapPost("", async (DataTableDto dataTableDto, IMediator mediator,
            LinkGenerator linker, HttpContext ctx, CancellationToken cancellationToken) =>
            {
                var lookups = dataTableDto.Lookups
                    .Select(l => new DataTableLookup(
                        l.LookupId,
                        l.ColumnName,
                        l.LookupDataTableId,
                        l.LookupValueColumn,
                        l.LookupDescriptionColumn,
                        l.LookupDisplayType))
                    .ToArray();
                var command = new CreateDataTableCommand(
                    DataTableName: dataTableDto.DataTableName,
                    DataTableDescription: dataTableDto.DataTableDescription,
                    TargetSchemaName: dataTableDto.TargetSchemaName,
                    TargetTableName: dataTableDto.TargetTableName,
                    ConnectionId: dataTableDto.ConnectionId,
                    CategoryId: dataTableDto.CategoryId,
                    AllowInsert: dataTableDto.AllowInsert,
                    AllowDelete: dataTableDto.AllowDelete,
                    AllowUpdate: dataTableDto.AllowUpdate,
                    AllowImport: dataTableDto.AllowImport,
                    DefaultEditorRowLimit: dataTableDto.DefaultEditorRowLimit,
                    LockedColumns: dataTableDto.LockedColumns,
                    LockedColumnsExcludeMode: dataTableDto.LockedColumnsExcludeMode,
                    HiddenColumns: dataTableDto.HiddenColumns,
                    ColumnOrder: dataTableDto.ColumnOrder,
                    Lookups: lookups);
                var dataTable = await mediator.SendAsync(command, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetDataTable", new { dataTableId = dataTable.DataTableId });
                return Results.Created(url, dataTable);
            })
            .ProducesValidationProblem()
            .Produces<MasterDataTable>()
            .WithSummary("Create data table")
            .WithDescription("Create a new data table")
            .WithName("CreateDataTable");
        
        group.MapPut("/{dataTableId:guid}", async (Guid dataTableId, DataTableDto dataTableDto, IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var lookups = dataTableDto.Lookups
                    .Select(l => new DataTableLookup(
                        l.LookupId,
                        l.ColumnName,
                        l.LookupDataTableId,
                        l.LookupValueColumn,
                        l.LookupDescriptionColumn,
                        l.LookupDisplayType))
                    .ToArray();
                var command = new UpdateDataTableCommand(
                    DataTableId: dataTableId,
                    DataTableName: dataTableDto.DataTableName,
                    DataTableDescription: dataTableDto.DataTableDescription,
                    TargetSchemaName: dataTableDto.TargetSchemaName,
                    TargetTableName: dataTableDto.TargetTableName,
                    ConnectionId: dataTableDto.ConnectionId,
                    CategoryId: dataTableDto.CategoryId,
                    AllowInsert: dataTableDto.AllowInsert,
                    AllowDelete: dataTableDto.AllowDelete,
                    AllowUpdate: dataTableDto.AllowUpdate,
                    AllowImport: dataTableDto.AllowImport,
                    DefaultEditorRowLimit: dataTableDto.DefaultEditorRowLimit,
                    LockedColumns: dataTableDto.LockedColumns,
                    LockedColumnsExcludeMode: dataTableDto.LockedColumnsExcludeMode,
                    HiddenColumns: dataTableDto.HiddenColumns,
                    ColumnOrder: dataTableDto.ColumnOrder,
                    Lookups: lookups);
                var dataTable = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(dataTable);
            })
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<MasterDataTable>()
            .WithSummary("Update data table")
            .WithDescription("Update an existing data table")
            .WithName("UpdateDataTable");
        
        group.MapDelete("/{dataTableId:guid}", async (Guid dataTableId, IMediator mediator,
            CancellationToken cancellationToken) =>
            {
                var command = new DeleteDataTableCommand(dataTableId);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Delete data table")
            .WithDescription("Delete a data table")
            .WithName("DeleteDataTable");
        
        group.MapPost("/categories", async (DataTableCategoryDto dataTableCategoryDto, IMediator mediator,
            LinkGenerator linker, HttpContext ctx, CancellationToken cancellationToken) =>
            {
                var command = new CreateDataTableCategoryCommand(dataTableCategoryDto.CategoryName);
                var category = await mediator.SendAsync(command, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetDataTableCategory", new { categoryId = category.CategoryId });
                return Results.Created(url, category);
            })
            .ProducesValidationProblem()
            .Produces<MasterDataTableCategory>()
            .WithSummary("Create data table category")
            .WithDescription("Create a new data table category")
            .WithName("CreateDataTableCategory");
        
        group.MapPut("/categories/{categoryId:guid}", async (Guid categoryId, DataTableCategoryDto dataTableCategoryDto,
                IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new UpdateDataTableCategoryCommand(
                    categoryId,
                    dataTableCategoryDto.CategoryName);
                var dataTable = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(dataTable);
            })
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<MasterDataTableCategory>()
            .WithSummary("Update data table category")
            .WithDescription("Update an existing data table category")
            .WithName("UpdateDataTableCategory");
        
        group.MapDelete("/categories/{categoryId:guid}", async (Guid categoryId, IMediator mediator,
            CancellationToken cancellationToken) =>
            {
                var command = new DeleteDataTableCategoryCommand(categoryId);
                await mediator.SendAsync(command, cancellationToken);
                return Results.NoContent();
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Delete data table category")
            .WithDescription("Delete a data table category. Tables in this category are not deleted.")
            .WithName("DeleteDataTableCategory");
    }
}