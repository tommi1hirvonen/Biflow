using Biflow.Core.Entities;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class ScheduleDeserializeTests
{
    private static readonly Schedule schedule = CreateSchedule();

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

    private static Schedule CreateSchedule()
    {
        var schedule = new Schedule
        {
            ScheduleName = "Test",
            CronExpression = "0 0 0 * * ?"
        };
        schedule.Tags.Add(new ScheduleTag("Test"));
        schedule.TagFilter.Add(new StepTag("Test"));
        return schedule.JsonRoundtrip(EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
    }
}
