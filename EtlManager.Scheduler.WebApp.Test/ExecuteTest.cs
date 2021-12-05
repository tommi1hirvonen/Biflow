using EtlManager.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Quartz;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManager.Scheduler.WebApp.Test;

[TestClass]
public class ExecuteTest
{
    [TestMethod]
    public async Task TestExecute()
    {
        var connectionString = "Server=localhost;Database=EtlManager;Integrated Security=true;MultipleActiveResultSets=true";

        var settings = new Dictionary<string, string>
        {
            { "ConnectionStrings:EtlManagerContext", connectionString },
            { "EtlManagerExecutorPath", @"C:\EtlManager\EtlManagerExecutor\EtlManagerExecutor.exe" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddLogging()
            .AddDbContextFactory<EtlManagerContext>(options =>
                    options.UseSqlServer(connectionString, o =>
                        o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)))
            .AddSingleton<ConsoleAppExecutionJob>()
            .BuildServiceProvider();

        var executionJob = services.GetService<ConsoleAppExecutionJob>();

        var jobId = Guid.Parse("6DE8C387-A5DB-4CC0-E489-08D98A13D18F");
        var scheduleId = Guid.Parse("59B417EE-22E6-46D9-D055-08D9A6B72D90");
        var jobContext = new MockJobContext(jobId, scheduleId);

        ArgumentNullException.ThrowIfNull(executionJob);

        await executionJob.Execute(jobContext);
    }
}

internal class MockJobContext : IJobExecutionContext
{
    private readonly MockJobDetail _jobDetail;
    private readonly MockTrigger _mockTrigger;

    public MockJobContext(Guid jobId, Guid scheduleId)
    {
        _jobDetail = new MockJobDetail(jobId);
        _mockTrigger = new MockTrigger(scheduleId);
    }

    public IJobDetail JobDetail => _jobDetail;

    public ITrigger Trigger => _mockTrigger;

    #region NotImplemented
    public IScheduler Scheduler => throw new NotImplementedException();

    public ICalendar? Calendar => throw new NotImplementedException();

    public bool Recovering => throw new NotImplementedException();

    public TriggerKey RecoveringTriggerKey => throw new NotImplementedException();

    public int RefireCount => throw new NotImplementedException();

    public JobDataMap MergedJobDataMap => throw new NotImplementedException();

    public IJob JobInstance => throw new NotImplementedException();

    public DateTimeOffset FireTimeUtc => throw new NotImplementedException();

    public DateTimeOffset? ScheduledFireTimeUtc => throw new NotImplementedException();

    public DateTimeOffset? PreviousFireTimeUtc => throw new NotImplementedException();

    public DateTimeOffset? NextFireTimeUtc => throw new NotImplementedException();

    public string FireInstanceId => throw new NotImplementedException();

    public object? Result { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public TimeSpan JobRunTime => throw new NotImplementedException();

    public CancellationToken CancellationToken => throw new NotImplementedException();

    public object? Get(object key)
    {
        throw new NotImplementedException();
    }

    public void Put(object key, object objectValue)
    {
        throw new NotImplementedException();
    }
    #endregion
}

internal class MockTrigger : ITrigger
{
    private readonly TriggerKey _triggerKey;

    public MockTrigger(Guid scheduleId)
    {
        _triggerKey = new TriggerKey(scheduleId.ToString());
    }

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

internal class MockJobDetail : IJobDetail
{
    private readonly JobKey _jobKey;

    public MockJobDetail(Guid jobId)
    {
        _jobKey = new JobKey(jobId.ToString());
    }

    #region NotImplemented
    public JobKey Key => _jobKey;

    public string? Description => throw new NotImplementedException();

    public Type JobType => throw new NotImplementedException();

    public JobDataMap JobDataMap => throw new NotImplementedException();

    public bool Durable => throw new NotImplementedException();

    public bool PersistJobDataAfterExecution => throw new NotImplementedException();

    public bool ConcurrentExecutionDisallowed => throw new NotImplementedException();

    public bool RequestsRecovery => throw new NotImplementedException();

    public IJobDetail Clone()
    {
        throw new NotImplementedException();
    }

    public JobBuilder GetJobBuilder()
    {
        throw new NotImplementedException();
    }
    #endregion
}