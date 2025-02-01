namespace Biflow.Ui.Core;

public record CreateDataTableCommand(
    string DataTableName,
    string? DataTableDescription,
    string TargetSchemaName,
    string TargetTableName,
    Guid ConnectionId,
    Guid? CategoryId,
    bool AllowInsert,
    bool AllowDelete,
    bool AllowUpdate,
    bool AllowImport,
    string[] LockedColumns,
    bool LockedColumnsExcludeMode,
    string[] HiddenColumns,
    string[] ColumnOrder,
    DataTableLookup[] Lookups) : IRequest<MasterDataTable>;

[UsedImplicitly]
internal class CreateDataTableCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    DataTableValidator validator)
    : IRequestHandler<CreateDataTableCommand, MasterDataTable>
{
    public async Task<MasterDataTable> Handle(CreateDataTableCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        if (!await dbContext.MsSqlConnections.AnyAsync(c => c.ConnectionId == request.ConnectionId, cancellationToken))
        {
            throw new NotFoundException<MsSqlConnection>(request.ConnectionId);
        }

        if (request.CategoryId is { } id &&
            !await dbContext.MasterDataTableCategories.AnyAsync(c => c.CategoryId == id, cancellationToken))
        {
            throw new NotFoundException<MasterDataTableCategory>(id);
        }
        
        // Check that a matching data table exists for all lookup table ids in request.
        var lookupTableIds = request.Lookups
            .Select(l => l.LookupDataTableId)
            .Distinct()
            .ToArray();
        var lookupTableIdsFromDb = await dbContext.MasterDataTables
            .Where(t => lookupTableIds.Contains(t.DataTableId))
            .Select(t => t.DataTableId)
            .ToArrayAsync(cancellationToken);
        foreach (var tableId in lookupTableIds)
        {
            if (!lookupTableIdsFromDb.Contains(tableId))
            {
                throw new NotFoundException<MasterDataTable>(tableId);
            }
        }

        var dataTable = new MasterDataTable
        {
            DataTableName = request.DataTableName,
            DataTableDescription = request.DataTableDescription,
            TargetSchemaName = request.TargetSchemaName,
            TargetTableName = request.TargetTableName,
            ConnectionId = request.ConnectionId,
            CategoryId = request.CategoryId,
            AllowInsert = request.AllowInsert,
            AllowDelete = request.AllowDelete,
            AllowUpdate = request.AllowUpdate,
            AllowImport = request.AllowImport,
            LockedColumns = request.LockedColumns.ToList(),
            LockedColumnsExcludeMode = request.LockedColumnsExcludeMode,
            HiddenColumns = request.HiddenColumns.ToList(),
            ColumnOrder = request.ColumnOrder.ToList()
        };

        foreach (var lookup in request.Lookups)
        {
            dataTable.Lookups.Add(new MasterDataTableLookup
            {
                LookupId = lookup.LookupId ?? Guid.Empty,
                ColumnName = lookup.ColumnName,
                LookupTableId = lookup.LookupDataTableId,
                LookupValueColumn = lookup.LookupValueColumn,
                LookupDescriptionColumn = lookup.LookupDescriptionColumn,
                LookupDisplayType = lookup.LookupDisplayType
            });
            lookup.EnsureDataAnnotationsValidated();
        }
        
        dataTable.EnsureDataAnnotationsValidated();
        await validator.EnsureValidatedAsync(dataTable, cancellationToken);
        
        dbContext.MasterDataTables.Add(dataTable);
        await dbContext.SaveChangesAsync(cancellationToken);

        return dataTable;
    }
}