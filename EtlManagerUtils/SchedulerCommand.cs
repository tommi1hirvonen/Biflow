namespace EtlManagerUtils
{
    public record SchedulerCommand(SchedulerCommand.CommandType Type, string? JobId, string? ScheduleId, string? CronExpression)
    {
        public enum CommandType
        {
            Add,
            Delete,
            Pause,
            Resume,
            Synchronize,
            Status
        }
    }
}
