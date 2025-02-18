namespace Biflow.Ui.Core;

public record UpdateDataTableCommand(
    Guid DataTableId,
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
internal class UpdateDataTableCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    DataTableValidator validator) : IRequestHandler<UpdateDataTableCommand, MasterDataTable>
{
    public async Task<MasterDataTable> Handle(UpdateDataTableCommand request, CancellationToken cancellationToken)
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
        
        var dataTable = await dbContext.MasterDataTables
            .Include(t => t.Lookups)
            .FirstOrDefaultAsync(t => t.DataTableId == request.DataTableId, cancellationToken)
            ?? throw new NotFoundException<MasterDataTable>(request.DataTableId);
        
        dataTable.DataTableName = request.DataTableName;
        dataTable.DataTableDescription = request.DataTableDescription;
        dataTable.TargetSchemaName = request.TargetSchemaName;
        dataTable.TargetTableName = request.TargetTableName;
        dataTable.ConnectionId = request.ConnectionId;
        dataTable.CategoryId = request.CategoryId;
        dataTable.AllowInsert = request.AllowInsert;
        dataTable.AllowDelete = request.AllowDelete;
        dataTable.AllowUpdate = request.AllowUpdate;
        dataTable.AllowImport = request.AllowImport;
        dataTable.LockedColumnsExcludeMode = request.LockedColumnsExcludeMode;

        if (!dataTable.ColumnOrder.SequenceEqual(request.ColumnOrder))
        {
            dataTable.ColumnOrder.Clear();
            dataTable.ColumnOrder.AddRange(request.ColumnOrder);
            dbContext.Entry(dataTable).Property(x => x.ColumnOrder).IsModified = true;
        }
        if (!dataTable.HiddenColumns.SequenceEqual(request.HiddenColumns))
        {
            dataTable.HiddenColumns = request.HiddenColumns.ToList();
            dbContext.Entry(dataTable).Property(x => x.HiddenColumns).IsModified = true;
        }
        if (!dataTable.LockedColumns.SequenceEqual(request.LockedColumns))
        {
            dataTable.LockedColumns = request.LockedColumns.ToList();
            dbContext.Entry(dataTable).Property(x => x.LockedColumns).IsModified = true;
        }
        
        // Synchronize lookups
        foreach (var updateLookup in dataTable.Lookups)
        {
            var lookup = request.Lookups.FirstOrDefault(l => l.LookupId == updateLookup.LookupId);
            if (lookup is null) continue;
            updateLookup.ColumnName = lookup.ColumnName;
            updateLookup.LookupTableId = lookup.LookupDataTableId;
            updateLookup.LookupValueColumn = lookup.LookupValueColumn;
            updateLookup.LookupDescriptionColumn = lookup.LookupDescriptionColumn;
            updateLookup.LookupDisplayType = lookup.LookupDisplayType;
        }
        var lookupsToRemove = dataTable.Lookups
            .Where(l1 => request.Lookups.All(l2 => l2.LookupId != l1.LookupId))
            .ToArray();
        foreach (var lookup in lookupsToRemove)
        {
            dataTable.Lookups.Remove(lookup);
        }
        var lookupsToAdd = request.Lookups.Where(l1 => dataTable.Lookups.All(l2 => l2.LookupId != l1.LookupId));
        foreach (var lookup in lookupsToAdd)
        {
            dataTable.Lookups.Add(new MasterDataTableLookup
            {
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
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return dataTable;
    }
}