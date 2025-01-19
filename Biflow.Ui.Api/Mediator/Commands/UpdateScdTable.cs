using Biflow.Core.Entities.Scd;
using Biflow.Ui.Core.Validation;

namespace Biflow.Ui.Api.Mediator.Commands;

internal record UpdateScdTableCommand(
    Guid ScdTableId,
    Guid ConnectionId,
    string ScdTableName,
    string SourceTableSchema,
    string SourceTableName,
    string TargetTableSchema,
    string TargetTableName,
    string? StagingTableSchema,
    string StagingTableName,
    string? PreLoadScript,
    string? PostLoadScript,
    bool FullLoad,
    bool ApplyIndexesOnCreate,
    bool SelectDistinct,
    string[] NaturalKeyColumns,
    SchemaDriftConfiguration SchemaDriftConfiguration) : IRequest<ScdTable>;

[UsedImplicitly]
internal class UpdateScdTableCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ScdTableValidator validator) : IRequestHandler<UpdateScdTableCommand, ScdTable>
{
    public async Task<ScdTable> Handle(UpdateScdTableCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var scdTable = await dbContext.ScdTables
            .FirstOrDefaultAsync(t => t.ScdTableId == request.ScdTableId, cancellationToken)
            ?? throw new NotFoundException<ScdTable>(request.ScdTableId);

        if (!await dbContext.SqlConnections.AnyAsync(c => c.ConnectionId == request.ConnectionId, cancellationToken))
        {
            throw new NotFoundException<SqlConnectionBase>(request.ConnectionId);
        }
        
        scdTable.ConnectionId = request.ConnectionId;
        scdTable.ScdTableName = request.ScdTableName;
        scdTable.SourceTableSchema = request.SourceTableSchema;
        scdTable.SourceTableName = request.SourceTableName;
        scdTable.TargetTableSchema = request.TargetTableSchema;
        scdTable.TargetTableName = request.TargetTableName;
        scdTable.StagingTableSchema = request.StagingTableSchema;
        scdTable.StagingTableName = request.StagingTableName;
        scdTable.PreLoadScript = request.PreLoadScript;
        scdTable.PostLoadScript = request.PostLoadScript;
        scdTable.FullLoad = request.FullLoad;
        scdTable.ApplyIndexesOnCreate = request.ApplyIndexesOnCreate;
        scdTable.SelectDistinct = request.SelectDistinct;
        scdTable.NaturalKeyColumns = request.NaturalKeyColumns.ToList();
        scdTable.SchemaDriftConfiguration = request.SchemaDriftConfiguration;
        
        scdTable.EnsureDataAnnotationsValidated();
        validator.EnsureValidated(scdTable);
        
        dbContext.Entry(scdTable).Property(x => x.SchemaDriftConfiguration).IsModified = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return scdTable;
    }
}