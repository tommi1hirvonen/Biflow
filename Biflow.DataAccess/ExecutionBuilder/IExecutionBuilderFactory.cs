using Biflow.DataAccess.Models;

namespace Biflow.DataAccess;

public interface IExecutionBuilderFactory
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="jobId"></param>
    /// <param name="createdBy"></param>
    /// <returns><see cref="ExecutionBuilder"/> if <see cref="Job"/> was found with the given <paramref name="jobId"/>, <see langword="null"/> if not</returns>
    public Task<ExecutionBuilder?> CreateAsync(Guid jobId, string createdBy, Guid[]? stepIdFilter = null);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="jobId"></param>
    /// <param name="createdBy"></param>
    /// <returns><see cref="ExecutionBuilder"/> if <see cref="Job"/> was found with the given <paramref name="jobId"/>, <see langword="null"/> if not</returns>
    public Task<ExecutionBuilder?> CreateAsync(Guid jobId, Guid scheduleId);
}
