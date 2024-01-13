using System.Linq.Expressions;

namespace Biflow.DataAccess;

public interface IExecutionBuilderFactory<TDbContext> where TDbContext : AppDbContext
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="jobId"></param>
    /// <param name="createdBy"></param>
    /// <param name="predicates">List of predicates to be applied as where clause predicates when translating the database query</param>
    /// <returns><see cref="ExecutionBuilder"/> if <see cref="Job"/> was found with the given <paramref name="jobId"/>, <see langword="null"/> if not</returns>
    public Task<ExecutionBuilder?> CreateAsync(Guid jobId, string? createdBy, params Func<TDbContext, Expression<Func<Step, bool>>>[] predicates);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="jobId"></param>
    /// <param name="createdBy"></param>
    /// <param name="parent">The parent <see cref="StepExecutionAttempt"/> that is creating/launching the built <see cref="Execution"/></param>
    /// <param name="predicates">List of predicates to be applied as where clause predicates when translating the database query</param>
    /// <returns><see cref="ExecutionBuilder"/> if <see cref="Job"/> was found with the given <paramref name="jobId"/>, <see langword="null"/> if not</returns>
    public Task<ExecutionBuilder?> CreateAsync(Guid jobId, string? createdBy, StepExecutionAttempt? parent, params Func<TDbContext, Expression<Func<Step, bool>>>[] predicates);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="jobId"></param>
    /// <param name="scheduleId"></param>
    /// <param name="predicates">List of predicates to be applied as where clause predicates when translating the database query</param>
    /// <returns><see cref="ExecutionBuilder"/> if <see cref="Job"/> was found with the given <paramref name="jobId"/>, <see langword="null"/> if not</returns>
    public Task<ExecutionBuilder?> CreateAsync(Guid jobId, Guid scheduleId, params Func<TDbContext, Expression<Func<Step, bool>>>[] predicates);
}
