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
    private static readonly string _connectionString =
        "Data Source=localhost;Database=BiflowTest;Integrated Security=sspi;Encrypt=true;TrustServerCertificate=true;";
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private static bool _databaseInitialized;

    public string Username { get; } = "testuser";

    public string Role { get; } = "Admin";

    public IDbContextFactory<AppDbContext> DbContextFactory { get; }

    public IExecutionBuilderFactory<AppDbContext> ExecutionBuilderFactory { get; }

    public DatabaseFixture()
    {
        var httpContextAccessor = new MockHttpContextAccessor(Username, Role);
        var settings = new Dictionary<string, string?>
        {
            { "ConnectionStrings:AppDbContext", _connectionString }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton<IHttpContextAccessor>(httpContextAccessor)
            .AddDbContextFactory<AppDbContext>()
            .AddExecutionBuilderFactory<AppDbContext>()
            .BuildServiceProvider();
        var factory = services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        var builderFactory = services.GetRequiredService<IExecutionBuilderFactory<AppDbContext>>();
        ArgumentNullException.ThrowIfNull(factory);
        (DbContextFactory, ExecutionBuilderFactory) = (factory, builderFactory);
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

            var respawner = await Respawner.CreateAsync(_connectionString);
            await respawner.ResetAsync(_connectionString);

            // Initialize seed data
            var context = await DbContextFactory.CreateDbContextAsync();


            #region SETTINGS
            var connection = new SqlConnectionInfo("Test connection", _connectionString);

            var appRegistration = new AppRegistration
            {
                AppRegistrationName = "Test app registration",
                ClientId = "some-client-id",
                ClientSecret = "some-client-secret",
                TenantId = "some-tenant-id"
            };

            var dataFactory = new DataFactory
            {
                AppRegistration = appRegistration,
                PipelineClientName = "Test Data Factory",
                SubscriptionId = "some-subscription-id",
                ResourceGroupName = "some-resource-group-name",
                ResourceName = "some-resource-name"
            };

            var synapseWorkspace = new SynapseWorkspace("some-workspace-url")
            {
                AppRegistration = appRegistration,
                PipelineClientName = "Test Synapse"
            };

            var functionApp = new FunctionApp
            {
                AppRegistration = appRegistration,
                FunctionAppName = "Test function app",
                SubscriptionId = "some-subscription-id",
                ResourceGroupName = "some-resource-group-name",
                ResourceName = "some-resource-name",
                FunctionAppKey = "somefunctionappkey"
            };
            #endregion

            #region JOB 1
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
            var jobParameter1 = new JobParameter
            {
                Job = job1,
                ParameterName = "JobParameter1",
                ParameterValueType = ParameterValueType.String,
                ValueString = "Hello world"
            };
            var jobParameter2 = new JobParameter
            {
                Job = job1,
                ParameterName = "JobParameter2",
                ParameterValueType = ParameterValueType.DateTime,
                ValueDateTime = DateTime.Now
            };
            var jobParameter3 = new JobParameter
            {
                Job = job1,
                ParameterName = "JobParameter3",
                ParameterValueType = ParameterValueType.Double,
                ValueDouble = 123.456
            };
            job1.JobParameters = [jobParameter1, jobParameter2, jobParameter3];
            var jobConcurrency = new JobConcurrency { Job = job1, StepType = StepType.Sql, MaxParallelSteps = 1 };
            job1.JobConcurrencies = [jobConcurrency];

            var tag1 = new Tag("Test tag") { Color = TagColor.DarkGray };
            var tag2 = new Tag("Another tag") { Color = TagColor.Red };

            var step1 = new SqlStep(job1.JobId)
            {
                StepName = "Test step 1",
                ExecutionPhase = 10,
                SqlStatement = "select 1",
                Connection = connection,
                Tags = [tag1, tag2]
            };

            var step2 = new SqlStep(job1.JobId)
            {
                StepName = "Test step 2",
                StepDescription = "Test step 2 description",
                ExecutionPhase = 20,
                SqlStatement = "select @param",
                Connection = connection,
                Tags = [tag1]
            };
            var step2Dependency = new Dependency(step2.StepId, step1.StepId) { Step = step2, DependantOnStep = step1, DependencyType = DependencyType.OnCompleted };
            step2.Dependencies = [step2Dependency];
            var step2Parameter = new SqlStepParameter
            {
                Step = step2,
                ParameterName = "@param",
                ParameterValueType = ParameterValueType.Int32,
                ValueInt32 = 10
            };
            step2.StepParameters = [step2Parameter];

            var step3 = new SqlStep(job1.JobId)
            {
                StepName = "Test step 3",
                ExecutionPhase = 20,
                SqlStatement = "select @param",
                Connection = connection,
                Tags = [tag1]
            };
            var step3Parameter = new SqlStepParameter
            {
                Step = step3,
                ParameterName = "@param",
                ParameterValueType = ParameterValueType.String,
                InheritFromJobParameter = jobParameter1
            };
            step3.StepParameters = [step3Parameter];

            var step4 = new SqlStep(job1.JobId)
            {
                StepName = "Test step 4",
                ExecutionPhase = 30,
                SqlStatement = "select @param",
                Connection = connection,
                Tags = [tag1]
            };
            var step4Dependency = new Dependency(step4.StepId, step3.StepId) { Step = step4, DependantOnStep = step3, DependencyType = DependencyType.OnSucceeded };
            step4.Dependencies = [step4Dependency];
            var step4Parameter = new SqlStepParameter
            {
                Step = step4,
                ParameterName = "@param",
                ParameterValueType = ParameterValueType.DateTime,
                UseExpression = true,
                Expression = new EvaluationExpression { Expression = "DateTime.Now" }
            };
            step4.StepParameters = [step4Parameter];

            var step3Target = new DataObject
            {
                ObjectUri = DataObject.CreateTableUri("TestServer", "TestDb", "TestSchema", "TestTable"),
                MaxConcurrentWrites = 1
            };

            var dataObjectLink_3_3 = new StepDataObject { DataObject = step3Target, Step = step3, ReferenceType = DataObjectReferenceType.Target };
            var dataObjectLink_3_4 = new StepDataObject { DataObject = step3Target, Step = step4, ReferenceType = DataObjectReferenceType.Source };
            step3Target.Steps = [dataObjectLink_3_3, dataObjectLink_3_4];

            step3.DataObjects = [dataObjectLink_3_3];

            var step4Target = new DataObject
            {
                ObjectUri = DataObject.CreateTableUri("TestServer", "TestDb", "TestSchema", "TestTable2"),
                MaxConcurrentWrites = 1
            };

            var dataObjectLink_4_4 = new StepDataObject { DataObject = step4Target, Step = step4, ReferenceType = DataObjectReferenceType.Target };
            step4Target.Steps = [dataObjectLink_4_4];

            step4.DataObjects = [dataObjectLink_4_4];

            job1.Steps = [step1, step2, step3, step4];
            #endregion

            #region JOB 2
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

            var step5 = new JobStep(job2.JobId)
            {
                StepName = "Test step 5",
                ExecutionPhase = 0,
                JobExecuteSynchronized = true,
                JobToExecute = job1,
                TagFilters = [tag2],
                Tags = []
            };

            var step6 = new JobStep(job2.JobId)
            {
                StepName = "Test step 6",
                ExecutionPhase = 0,
                JobExecuteSynchronized = true,
                JobToExecute = job1,
                TagFilters = [tag1, tag2],
                Tags = []
            };

            var step7 = new SqlStep(job2.JobId)
            {
                IsEnabled = false,
                StepName = "Test step 7",
                ExecutionPhase = 30,
                SqlStatement = "select 1",
                Connection = connection,
                Tags = [tag1]
            };

            var step8 = new SqlStep(job2.JobId)
            {
                StepName = "Test step 8",
                ExecutionPhase = 30,
                SqlStatement = "select 1",
                Connection = connection,
                Tags = [tag1]
            };

            var step9 = new PipelineStep(job2.JobId)
            {
                StepName = "Test step 9",
                ExecutionPhase = 35,
                PipelineClient = dataFactory,
                PipelineName = "test pipeline",
                Tags = []
            };

            var step10 = new PipelineStep(job2.JobId)
            {
                StepName = "Test step 10",
                ExecutionPhase = 40,
                PipelineClient = synapseWorkspace,
                PipelineName = "test pipeline 2",
                Tags = []
            };

            var step11 = new FunctionStep(job2.JobId)
            {
                StepName = "Test step 11",
                ExecutionPhase = 45,
                FunctionApp = functionApp,
                FunctionInput = "test-input",
                FunctionUrl = "http://function-url.com/test-function",
                Tags = []
            };

            job2.Steps = [step5, step6, step7, step8, step9, step10, step11];
            #endregion

            #region SCHEDULES
            var schedule1 = new Schedule(job1.JobId)
            {
                Job = job1,
                ScheduleName = "Test schedule",
                CronExpression = "",
                Tags = []
            };
            var schedule2 = new Schedule(job2.JobId)
            {
                Job = job2,
                ScheduleName = "Another schedule",
                CronExpression = "",
                Tags = [tag1]
            };
            #endregion

            context.AddRange(job1, job2, schedule1, schedule2);
            await context.SaveChangesAsync();

            _databaseInitialized = true;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
