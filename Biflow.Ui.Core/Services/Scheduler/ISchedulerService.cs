using Biflow.DataAccess.Models;
using OneOf;

namespace Biflow.Ui.Core;

public interface ISchedulerService
{
    public Task DeleteJobAsync(Job job);

    public Task<SchedulerStatusResponse> GetStatusAsync();

    public Task AddScheduleAsync(Schedule schedule);

    public Task RemoveScheduleAsync(Schedule schedule);

    public Task UpdateScheduleAsync(Schedule schedule);

    public Task SynchronizeAsync();

    public Task ToggleScheduleEnabledAsync(Schedule schedule, bool enabled);
}

[GenerateOneOf]
public partial class SchedulerStatusResponse : OneOfBase<Success, AuthorizationError, SchedulerError, UndefinedError> { }

public readonly record struct Success();

public readonly record struct AuthorizationError();

public readonly record struct SchedulerError();

public readonly record struct UndefinedError();