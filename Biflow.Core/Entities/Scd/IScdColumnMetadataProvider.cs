namespace Biflow.Core.Entities;

public interface IScdColumnMetadataProvider
{
    public Task<IReadOnlyList<ScdColumnMetadata>> GetTableColumnsAsync(
        string connectionString, string schema, string table, CancellationToken cancellationToken = default);
}