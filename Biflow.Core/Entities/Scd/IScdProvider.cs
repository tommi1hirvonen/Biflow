namespace Biflow.Core.Entities.Scd;

public interface IScdProvider
{
    public Task<string> CreateDataLoadStatementAsync(CancellationToken cancellationToken = default);

    public Task<string> CreateStructureUpdateStatementAsync(CancellationToken cancellationToken = default);
}