using Biflow.Core.Entities;
using System.Text.Json;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class ScheduleDeserializeTests
{
    private static readonly Schedule schedule = GetDeserializedSchedule();

    [Fact]
    public void Tags_NotEmpty()
    {
        Assert.NotEmpty(schedule.Tags);
    }

    [Fact]
    public void TagFilter_NotEmpty()
    {
        Assert.NotEmpty(schedule.TagFilter);
    }


    private static Schedule GetDeserializedSchedule()
    {
        var json = JsonSerializer.Serialize(CreateSchedule(), EnvironmentSnapshot.JsonSerializerOptions);
        var schedule = JsonSerializer.Deserialize<Schedule>(json, EnvironmentSnapshot.JsonSerializerOptions);
        ArgumentNullException.ThrowIfNull(schedule);
        return schedule;
    }

    private static Schedule CreateSchedule()
    {
        var schedule = new Schedule
        {
            ScheduleName = "Test",
            CronExpression = "0 0 0 * * ?"
        };
        schedule.Tags.Add(new ScheduleTag("Test"));
        schedule.TagFilter.Add(new StepTag("Test"));
        return schedule;
    }
}
