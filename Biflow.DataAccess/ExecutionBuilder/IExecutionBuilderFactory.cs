using Biflow.DataAccess.Models;

namespace Biflow.DataAccess;

public interface IExecutionBuilderFactory
{
    public Task<ExecutionBuilder> CreateAsync(Guid jobId, string createdBy);

    public Task<ExecutionBuilder> CreateAsync(Guid jobId, Guid scheduleId);
}
