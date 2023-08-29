using Biflow.DataAccess.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Respawn;
using Xunit;

namespace Biflow.DataAccess.Test;

public class DatabaseFixture : IAsyncLifetime
{
    private string ConnectionString { get; } =
        "Data Source=localhost;Database=BiflowTest;Integrated Security=sspi;Encrypt=true;TrustServerCertificate=true;";

    public string Username { get; } = "testuser";

    public string Role { get; } = "Admin";

    public IDbContextFactory<BiflowContext> DbContextFactory { get; }

    public IExecutionBuilderFactory ExecutionBuilderFactory { get; }

    public DatabaseFixture()
    {
        var httpContextAccessor = new MockHttpContextAccessor(Username, Role);
        var settings = new Dictionary<string, string?>
        {
            { "ConnectionStrings:BiflowContext", ConnectionString }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton<IHttpContextAccessor>(httpContextAccessor)
            .AddDbContextFactory<BiflowContext>()
            .AddExecutionBuilderFactory()
            .BuildServiceProvider();
        var factory = services.GetRequiredService<IDbContextFactory<BiflowContext>>();
        var builderFactory = services.GetRequiredService<IExecutionBuilderFactory>();
        ArgumentNullException.ThrowIfNull(factory);
        (DbContextFactory, ExecutionBuilderFactory) = (factory, builderFactory);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public async Task InitializeAsync()
    {
        var respawner = await Respawner.CreateAsync(ConnectionString);
        await respawner.ResetAsync(ConnectionString);

        // Initialize seed data
        var context = await DbContextFactory.CreateDbContextAsync();
        var connection = new SqlConnectionInfo("Test connection", ConnectionString);

        var job1 = new Job
        {
            JobName = "Test job",
            JobDescription = "Test job",
            UseDependencyMode = true,
            StopOnFirstError = true,
            MaxParallelSteps = 4,
            OvertimeNotificationLimitMinutes = 120,
            Category = new JobCategory { CategoryName = "Test category" }
        };
        var jobParameter = new JobParameter
        {
            Job = job1,
            ParameterName = "JobParameter1",
            ParameterValueType = ParameterValueType.String,
            ValueString = "Hello world"
        };
        job1.JobParameters = new List<JobParameter> { jobParameter };
        var jobConcurrency = new JobConcurrency { Job = job1, StepType = StepType.Sql, MaxParallelSteps = 1 };
        job1.JobConcurrencies = new List<JobConcurrency> { jobConcurrency };

        var tag1 = new Tag("Test tag") { Color = TagColor.DarkGray };
        var tag2 = new Tag("Another tag") { Color = TagColor.Red };

        var step1 = new SqlStep
        {
            JobId = job1.JobId,
            StepName = "Test step 1",
            ExecutionPhase = 10,
            SqlStatement = "select 1",
            Connection = connection,
            Tags = new List<Tag> { tag1, tag2 }
        };

        var step2 = new SqlStep
        {
            JobId = job1.JobId,
            StepName = "Test step 2",
            StepDescription = "Test step 2 description",
            ExecutionPhase = 20,
            SqlStatement = "select @param",
            Connection = connection,
            Tags = new List<Tag> { tag1 }
        };
        var step2Dependency = new Dependency(step2.StepId, step1.StepId) { Step = step2, DependantOnStep = step1, DependencyType = DependencyType.OnCompleted };
        step2.Dependencies = new List<Dependency> { step2Dependency };
        var step2Parameter = new SqlStepParameter
        {
            Step = step2,
            ParameterName = "@param",
            ParameterValueType = ParameterValueType.Int32,
            ValueInt32 = 10
        };
        step2.StepParameters = new List<SqlStepParameter> { step2Parameter };

        var step3 = new SqlStep
        {
            JobId = job1.JobId,
            StepName = "Test step 3",
            ExecutionPhase = 20,
            SqlStatement = "select @param",
            Connection = connection,
            Tags = new List<Tag> { tag1 }
        };
        var step3Parameter = new SqlStepParameter
        {
            Step = step3,
            ParameterName = "@param",
            ParameterValueType = ParameterValueType.String,
            InheritFromJobParameter = jobParameter
        };
        step3.StepParameters = new List<SqlStepParameter> { step3Parameter };

        var step4 = new SqlStep
        {
            JobId = job1.JobId,
            StepName = "Test step 4",
            ExecutionPhase = 30,
            SqlStatement = "select @param",
            Connection = connection,
            Tags = new List<Tag> { tag1 }
        };
        var step4Dependency = new Dependency(step4.StepId, step3.StepId) { Step = step4, DependantOnStep = step3, DependencyType = DependencyType.OnSucceeded };
        step4.Dependencies = new List<Dependency> { step4Dependency };
        var step4Parameter = new SqlStepParameter
        {
            Step = step4,
            ParameterName = "@param",
            ParameterValueType = ParameterValueType.DateTime,
            UseExpression = true,
            Expression = new EvaluationExpression { Expression = "DateTime.Now" }
        };
        step4.StepParameters = new List<SqlStepParameter> { step4Parameter };

        var step3Target = new DataObject
        {
            ServerName = "TestServer",
            DatabaseName = "TestDb",
            SchemaName = "TestSchema",
            ObjectName = "TestTable",
            MaxConcurrentWrites = 1,
            Writers = new List<Step> { step3 },
            Readers = new List<Step> { step4 }
        };
        step3.Targets = new List<DataObject> { step3Target };
        step4.Sources = new List<DataObject> { step3Target };

        var step4Target = new DataObject
        {
            ServerName = "TestServer",
            DatabaseName = "TestDb",
            SchemaName = "TestSchema",
            ObjectName = "TestTable2",
            MaxConcurrentWrites = 1,
            Writers = new List<Step> { step4 }
        };
        step4.Targets = new List<DataObject> { step4Target };

        job1.Steps = new List<Step> { step1, step2, step3, step4 };

        var job2 = new Job
        {
            JobName = "Another job",
            JobDescription = "Another job",
            UseDependencyMode = false,
            StopOnFirstError = true,
            MaxParallelSteps = 5,
            OvertimeNotificationLimitMinutes = 0,
            Category = null
        };

        var step5 = new JobStep
        {
            JobId = job2.JobId,
            StepName = "Test step 5",
            ExecutionPhase = 0,
            JobExecuteSynchronized = true,
            JobToExecute = job1,
            TagFilters = new List<Tag> { tag2 },
            Tags = new List<Tag>()
        };

        var step6 = new JobStep
        {
            JobId = job2.JobId,
            StepName = "Test step 6",
            ExecutionPhase = 0,
            JobExecuteSynchronized = true,
            JobToExecute = job1,
            TagFilters = new List<Tag> { tag1, tag2 },
            Tags = new List<Tag>()
        };

        var step7 = new SqlStep
        {
            IsEnabled = false,
            JobId = job2.JobId,
            StepName = "Test step 7",
            ExecutionPhase = 30,
            SqlStatement = "select 1",
            Connection = connection,
            Tags = new List<Tag> { tag1 }
        };

        var step8 = new SqlStep
        {
            JobId = job2.JobId,
            StepName = "Test step 8",
            ExecutionPhase = 30,
            SqlStatement = "select 1",
            Connection = connection,
            Tags = new List<Tag> { tag1 }
        };

        job2.Steps = new List<Step> { step5, step6, step7, step8 };

        var schedule1 = new Schedule(job1.JobId)
        {
            Job = job1,
            IsEnabled = true,
            ScheduleName = "Test schedule",
            CronExpression = "",
            Tags = new List<Tag>()
        };
        var schedule2 = new Schedule(job2.JobId)
        {
            Job = job2,
            IsEnabled = true,
            ScheduleName = "Another schedule",
            CronExpression = "",
            Tags = new List<Tag>() { tag1 }
        };

        context.AddRange(job1, job2, schedule1, schedule2);
        await context.SaveChangesAsync();
    }

}
