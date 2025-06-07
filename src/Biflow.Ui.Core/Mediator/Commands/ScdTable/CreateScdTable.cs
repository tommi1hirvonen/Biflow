using Biflow.Core.Entities.Scd;
using Biflow.Ui.Core.Validation;

namespace Biflow.Ui.Core;

public record CreateScdTableCommand(
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
internal class CreateScdTableCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ScdTableValidator validator) : IRequestHandler<CreateScdTableCommand, ScdTable>
{
    public async Task<ScdTable> Handle(CreateScdTableCommand request, CancellationToken cancellationToken)
    {
        var table = new ScdTable
        {
            ConnectionId = request.ConnectionId,
            ScdTableName = request.ScdTableName,
            SourceTableSchema = request.SourceTableSchema,
            SourceTableName = request.SourceTableName,
            TargetTableSchema = request.TargetTableSchema,
            TargetTableName = request.TargetTableName,
            StagingTableSchema = request.StagingTableSchema,
            StagingTableName = request.StagingTableName,
            PreLoadScript = request.PreLoadScript,
            PostLoadScript = request.PostLoadScript,
            FullLoad = request.FullLoad,
            ApplyIndexesOnCreate = request.ApplyIndexesOnCreate,
            SelectDistinct = request.SelectDistinct,
            NaturalKeyColumns = request.NaturalKeyColumns.ToList(),
            SchemaDriftConfiguration = request.SchemaDriftConfiguration
        };
        table.EnsureDataAnnotationsValidated();
        await validator.EnsureValidatedAsync(table, cancellationToken);
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.ScdTables.Add(table);
        await dbContext.SaveChangesAsync(cancellationToken);
        return table;
    }
}