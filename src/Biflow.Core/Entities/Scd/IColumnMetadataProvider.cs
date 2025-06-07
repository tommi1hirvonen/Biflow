namespace Biflow.Core.Entities.Scd;

public interface IColumnMetadataProvider
{
    public Task<IReadOnlyList<FullColumnMetadata>> GetColumnsAsync(
        string schema, string table, CancellationToken cancellationToken = default);
}