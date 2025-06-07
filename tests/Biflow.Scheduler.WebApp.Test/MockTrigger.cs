using Quartz;

namespace Biflow.Scheduler.WebApp.Test;

internal class MockTrigger(Guid scheduleId) : ITrigger
{
    private readonly TriggerKey _triggerKey = new(scheduleId.ToString());

    public TriggerKey Key => _triggerKey;

    #region NotImplemented
    public JobKey JobKey => throw new NotImplementedException();

    public string? Description => throw new NotImplementedException();

    public string? CalendarName => throw new NotImplementedException();

    public JobDataMap JobDataMap => throw new NotImplementedException();

    public DateTimeOffset? FinalFireTimeUtc => throw new NotImplementedException();

    public int MisfireInstruction => throw new NotImplementedException();

    public DateTimeOffset? EndTimeUtc => throw new NotImplementedException();

    public DateTimeOffset StartTimeUtc => throw new NotImplementedException();

    public int Priority { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public bool HasMillisecondPrecision => throw new NotImplementedException();

    public ITrigger Clone()
    {
        throw new NotImplementedException();
    }

    public int CompareTo(ITrigger? other)
    {
        throw new NotImplementedException();
    }

    public DateTimeOffset? GetFireTimeAfter(DateTimeOffset? afterTime)
    {
        throw new NotImplementedException();
    }

    public bool GetMayFireAgain()
    {
        throw new NotImplementedException();
    }

    public DateTimeOffset? GetNextFireTimeUtc()
    {
        throw new NotImplementedException();
    }

    public DateTimeOffset? GetPreviousFireTimeUtc()
    {
        throw new NotImplementedException();
    }

    public IScheduleBuilder GetScheduleBuilder()
    {
        throw new NotImplementedException();
    }

    public TriggerBuilder GetTriggerBuilder()
    {
        throw new NotImplementedException();
    }
    #endregion
}
