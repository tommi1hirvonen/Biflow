namespace Biflow.Core.Entities;

public interface IScdProvider
{
    public Task<string> CreateDataLoadStatementAsync(CancellationToken cancellationToken = default);
    
    public Task<string> CreateStructureUpdateStatementAsync(CancellationToken cancellationToken = default);
}