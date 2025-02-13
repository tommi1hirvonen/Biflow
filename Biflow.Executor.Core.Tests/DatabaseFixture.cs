using Biflow.Core;
using Biflow.Core.Entities;
using Biflow.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Biflow.Executor.Core.Test;

public class DatabaseFixture : IAsyncLifetime
{
    private static readonly string _connectionString =
        "Data Source=localhost;Database=BiflowTest;Integrated Security=sspi;Encrypt=true;TrustServerCertificate=true;";
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private static bool _databaseInitialized;

    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory;
    private readonly IExecutionBuilderFactory<ExecutorDbContext> _executionBuilderFactory;

    public IServiceProvider Services { get; }

    public DatabaseFixture()
    {
        var settings = new Dictionary<string, string?>
        {
            { "ConnectionStrings:AppDbContext", _connectionString },
            { "PollingIntervalMs", "5000" },
            { "EmailSettings:SmtpServer", "" },
            { "EmailSettings:EnableSsl", "true" },
            { "EmailSettings:Port", "587" },
            { "EmailSettings:FromAddress", "" },
            { "EmailSettings:AnonymousAuthentication", "false" },
            { "EmailSettings:Username", "" },
            { "EmailSettings:Password", "" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddExecutorServices(configuration)
            .BuildServiceProvider();
        Services = services;
        _dbContextFactory = services.GetRequiredService<IDbContextFactory<ExecutorDbContext>>();
        _executionBuilderFactory = services.GetRequiredService<IExecutionBuilderFactory<ExecutorDbContext>>();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public async Task InitializeAsync()
    {
        try
        {
            await _semaphore.WaitAsync(); // Synchronize access

            if (_databaseInitialized)
            {
                return;
            }

            // Initialize seed data
            var context = await _dbContextFactory.CreateDbContextAsync();
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();

            var sqlConnection1 = new MsSqlConnection
            {
                ConnectionName = "Test SQL connection",
                ConnectionString = _connectionString
            };

            var job1 = new Job
            {
                JobName = "Test job 1",
                JobDescription = "Test job 1",
                ExecutionMode = ExecutionMode.Dependency,
                StopOnFirstError = true,
                MaxParallelSteps = 4,
                OvertimeNotificationLimitMinutes = 120
            };
            var jobParameter1 = new JobParameter
            {
                Job = job1,
                ParameterName = "JobParameter1",
                ParameterValue = new("Hello world")
            };
            job1.JobParameters.Add(jobParameter1);
            var jobConcurrency = new JobConcurrency { Job = job1, StepType = StepType.Sql, MaxParallelSteps = 1 };
            job1.JobConcurrencies.Add(jobConcurrency);

            var tag1 = new StepTag("test-tag-1") { Color = TagColor.DarkGray };
            var tag2 = new StepTag("test-tag-2") { Color = TagColor.LightGray };

            var step1 = new SqlStep
            {
                StepName = "Test step 1",
                ExecutionPhase = 10,
                SqlStatement = "select 1",
                Connection = sqlConnection1
            };
            step1.Tags.AddRange([tag1, tag2]);

            var step2 = new SqlStep
            {
                StepName = "Test step 2",
                StepDescription = "Test step 2",
                ExecutionPhase = 20,
                SqlStatement = "select @param",
                Connection = sqlConnection1
            };
            step2.Tags.Add(tag1);
            var step2Dependency = new Dependency
            {
                StepId = step2.StepId,
                Step = step2,
                DependantOnStepId = step1.StepId,
                DependantOnStep = step1,
                DependencyType = DependencyType.OnCompleted
            };
            step2.Dependencies.Add(step2Dependency);
            var step2Parameter = new SqlStepParameter
            {
                Step = step2,
                ParameterName = "@param",
                ParameterValue = new(10),
                InheritFromJobParameter = jobParameter1
            };
            step2.StepParameters.Add(step2Parameter);

            job1.Steps.AddRange([step1, step2]);

            var schedule1 = new Schedule
            {
                JobId = job1.JobId,
                Job = job1,
                ScheduleName = "Test schedule 1",
                CronExpression = ""
            };

            var user1 = new User
            {
                Username = "admin",
                Email = "",
                AuthorizeAllDataTables = true,
                AuthorizeAllJobs = true
            };
            user1.SetIsAdmin();

            var sub1 = new JobSubscription(user1.UserId, job1.JobId)
            {
                User = user1,
                Job = job1,
                AlertType = AlertType.OnCompletion
            };
            var sub2 = new JobStepTagSubscription(user1.UserId, job1.JobId, tag1.TagId)
            {
                User = user1,
                Job = job1,
                AlertType = AlertType.OnCompletion,
                Tag = tag1
            };
            var sub3 = new StepSubscription(user1.UserId, step1.StepId)
            {
                User = user1,
                Step = step1,
                AlertType = AlertType.OnCompletion
            };
            var sub4 = new StepTagSubscription(user1.UserId, tag1.TagId)
            {
                User = user1,
                Tag = tag1,
                AlertType = AlertType.OnCompletion
            };

            user1.Subscriptions.AddRange([sub1, sub2, sub3, sub4]);

            context.AddRange(job1, schedule1, user1);
            await context.SaveChangesAsync();

            var executionBuilder1 = await _executionBuilderFactory.CreateAsync(job1.JobId, "admin");
            ArgumentNullException.ThrowIfNull(executionBuilder1);
            executionBuilder1.AddAll();
            await executionBuilder1.SaveExecutionAsync();

            _databaseInitialized = true;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
