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
    public string ConnectionString { get; } =
        "Data Source=localhost;Database=BiflowTest;Integrated Security=sspi;Encrypt=true;TrustServerCertificate=true;";

    public string Username { get; } = "testuser";

    public string Role { get; } = "Admin";

    public IDbContextFactory<BiflowContext> DbContextFactory { get; }

    public DatabaseFixture()
    {
        var httpContextAccessor = new MockHttpContextAccessor(Username, Role);
        var settings = new Dictionary<string, string?>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton<IHttpContextAccessor>(httpContextAccessor)
            .AddDbContextFactory<BiflowContext>(options =>
            {
                options.EnableSensitiveDataLogging();
                options.UseSqlServer(ConnectionString, o =>
                {
                    o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                });
            })
            .BuildServiceProvider();
        var factory = services.GetService<IDbContextFactory<BiflowContext>>();
        ArgumentNullException.ThrowIfNull(factory);
        DbContextFactory = factory;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public async Task InitializeAsync()
    {
        var respawner = await Respawner.CreateAsync(ConnectionString);
        await respawner.ResetAsync(ConnectionString);

        // Initialize seed data
        var context = await DbContextFactory.CreateDbContextAsync();
        var connection = new SqlConnectionInfo("Test connection", ConnectionString);

        var job = new Job
        {
            JobName = "Test job",
            JobDescription = "Test job",
            UseDependencyMode = true,
            StopOnFirstError = true,
            MaxParallelSteps = 4,
            OvertimeNotificationLimitMinutes = 120,
            IsEnabled = true,
            Category = new JobCategory { CategoryName = "Test category" }
        };
        var jobParameter = new JobParameter
        {
            Job = job,
            ParameterName = "JobParameter1",
            ParameterValueType = ParameterValueType.String,
            ValueString = "Hello world"
        };
        job.JobParameters = new List<JobParameter> { jobParameter };
        var jobConcurrency = new JobConcurrency { Job = job, StepType = StepType.Sql, MaxParallelSteps = 1 };
        job.JobConcurrencies = new List<JobConcurrency> { jobConcurrency };

        var tag = new Tag("Test tag") { Color = TagColor.DarkGray };

        var step1 = new SqlStep
        {
            JobId = job.JobId,
            StepName = "Test step 1",
            ExecutionPhase = 10,
            SqlStatement = "select 1",
            Connection = connection,
            Tags = new List<Tag> { tag }
        };

        var step2 = new SqlStep
        {
            JobId = job.JobId,
            StepName = "Test step 2",
            StepDescription = "Test step 2 description",
            ExecutionPhase = 20,
            SqlStatement = "select @param",
            Connection = connection,
            Tags = new List<Tag> { tag }
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
            JobId = job.JobId,
            StepName = "Test step 3",
            ExecutionPhase = 20,
            SqlStatement = "select @param",
            Connection = connection,
            Tags = new List<Tag> { tag }
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
            JobId = job.JobId,
            StepName = "Test step 4",
            ExecutionPhase = 30,
            SqlStatement = "select @param",
            Connection = connection,
            Tags = new List<Tag> { tag }
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

        job.Steps = new List<Step> { step1, step2, step3, step4 };

        context.AddRange(job);
        await context.SaveChangesAsync();
    }

}
