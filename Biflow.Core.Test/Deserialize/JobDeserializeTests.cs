using Biflow.Core.Entities;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class JobDeserializeTests
{
    private static readonly Job deserializedJob = CreateJob();

    [Fact]
    public void Parameters_NotEmpty()
    {
        Assert.NotEmpty(deserializedJob.JobParameters);
    }

    [Fact]
    public void Concurrencies_NotEmpty()
    {
        Assert.NotEmpty(deserializedJob.JobConcurrencies);
    }

    [Fact]
    public void Schedules_NotEmpty()
    {
        Assert.NotEmpty(deserializedJob.Schedules);
    }

    [Fact]
    public void Steps_NotEmpty()
    {
        Assert.NotEmpty(deserializedJob.Steps);
    }

    [Fact]
    public void Tags_NotEmpty()
    {
        Assert.NotEmpty(deserializedJob.Tags);
    }

    private static Job CreateJob()
    {
        var job = new Job
        {
            JobName = "Test",
        };
        job.JobParameters.Add(new JobParameter
        {
            ParameterName = "Test",
            ParameterValue = new ParameterValue(123.456)
        });
        job.JobConcurrencies.Add(new JobConcurrency
        {
            StepType = StepType.Sql,
            MaxParallelSteps = 1,
        });
        job.Schedules.Add(new Schedule
        {
            ScheduleName = "Test",
            CronExpression = "0 0 0 * * ?"
        });
        job.Steps.Add(new ExeStep
        {
            StepName = "Test"
        });
        job.Tags.Add(new JobTag("Test"));
        return job.JsonRoundtrip(EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
    }
}
