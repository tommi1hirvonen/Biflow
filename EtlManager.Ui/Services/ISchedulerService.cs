using EtlManager.DataAccess.Models;
using EtlManager.Utilities;

namespace EtlManager.Ui;

public interface ISchedulerService
{
    public Task<bool> DeleteJobAsync(Job job);

    public Task<(bool Running, bool Error, string Status)> GetStatusAsync();

    public Task<bool> SendCommandAsync(SchedulerCommand.CommandType commandType, Schedule? schedule);

    public Task<bool> SynchronizeAsync();

    public Task<bool> ToggleScheduleEnabledAsync(Schedule schedule, bool enabled);
}
