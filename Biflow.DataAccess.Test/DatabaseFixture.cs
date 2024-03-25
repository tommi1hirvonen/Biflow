using Biflow.Core;
using Biflow.Core.Entities;
using Biflow.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Biflow.DataAccess.Test;

public class DatabaseFixture : IAsyncLifetime
{
    private static readonly string _connectionString =
        "Data Source=localhost;Database=BiflowTest;Integrated Security=sspi;Encrypt=true;TrustServerCertificate=true;";
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private static bool _databaseInitialized;
    private readonly ITokenService _tokenService;
    private readonly IHttpClientFactory _httpClientFactory;

    public string Username { get; } = "testuser";

    public string Role { get; } = "Admin";

    public IDbContextFactory<AppDbContext> DbContextFactory { get; }

    public IExecutionBuilderFactory<AppDbContext> ExecutionBuilderFactory { get; }

    public JobDuplicatorFactory JobDuplicatorFactory { get; }

    public StepsDuplicatorFactory StepsDuplicatorFactory { get; }

    public DatabaseFixture()
    {
        var userService = new MockUserService(Username, Role);   
        var settings = new Dictionary<string, string?>
        {
            { "ConnectionStrings:AppDbContext", _connectionString }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
        var services = new ServiceCollection()
            .AddHttpClient()
            .AddSingleton<ITokenService, TokenService<AppDbContext>>()
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton<IUserService>(userService)
            .AddDbContextFactory<AppDbContext>()
            .AddExecutionBuilderFactory<AppDbContext>()
            .AddDuplicatorServices()
            .BuildServiceProvider();
        var dbContextFactory = services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        var executionBuilderFactory = services.GetRequiredService<IExecutionBuilderFactory<AppDbContext>>();
        var jobDuplicatoryFactory = services.GetRequiredService<JobDuplicatorFactory>();
        var stepsDuplicatoryFactory = services.GetRequiredService<StepsDuplicatorFactory>();
        DbContextFactory = dbContextFactory;
        ExecutionBuilderFactory = executionBuilderFactory;
        JobDuplicatorFactory = jobDuplicatoryFactory;
        StepsDuplicatorFactory = stepsDuplicatoryFactory;
        _tokenService = services.GetRequiredService<ITokenService>();
        _httpClientFactory = services.GetRequiredService<IHttpClientFactory>();
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
            var context = await DbContextFactory.CreateDbContextAsync();
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();

            #region SETTINGS
            var sqlConnection = new SqlConnectionInfo
            {
                ConnectionName = "Test SQL connection",
                ConnectionString = _connectionString
            };

            var asConnection = new AnalysisServicesConnectionInfo
            {
                ConnectionName = "Test AS connection",
                ConnectionString = "Data Source=localhost;Password=asd"
            };

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

            var synapseWorkspace = new SynapseWorkspace
            {
                AppRegistration = appRegistration,
                PipelineClientName = "Test Synapse",
                SynapseWorkspaceUrl = "some-workspace-url"
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

            var qlikClient = new QlikCloudClient
            {
                QlikCloudClientName = "Test Qlik Cloud Client",
                EnvironmentUrl = "https://test-qlik-url.com",
                ApiToken = "some-api-token"
            };

            var blobClient1 = new BlobStorageClient
            {
                BlobStorageClientName = "Test blob storage client"
            };
            blobClient1.UseAppRegistration(appRegistration, "https://some-storage-account-url.com/");

            var blobClient2 = new BlobStorageClient
            {
                BlobStorageClientName = "Test blob storage client 2"
            };
            blobClient2.UseUrl("https://some-storage-account-url.com?sig=asdasd");

            var blobClient3 = new BlobStorageClient
            {
                BlobStorageClientName = "Test blob storage client 3"
            };
            blobClient3.UseConnectionString("some-connection-string");

            var credential = new Credential
            {
                Domain = ".",
                Username = "TestUser",
                Password = "Strong_Password!9000#"
            };

            var apiKey = new ApiKey
            {
                Name = "Test API key",
                ValidFrom = DateTimeOffset.Now,
                ValidTo = DateTimeOffset.Now.AddYears(2)
            };
            #endregion

            #region JOB 1
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
            var jobParameter4 = new JobParameter
            {
                Job = job1,
                ParameterName = "JobParameter4",
                ParameterValueType = ParameterValueType.String,
                UseExpression = true,
                Expression = new()
                {
                    Expression = """
                    $"{100 + 23}-{400 + 56}"
                    """
                }
            };
            job1.JobParameters.AddRange([jobParameter1, jobParameter2, jobParameter3, jobParameter4]);
            var jobConcurrency = new JobConcurrency { Job = job1, StepType = StepType.Sql, MaxParallelSteps = 1 };
            job1.JobConcurrencies.Add(jobConcurrency);

            var tag1 = new StepTag("Test tag") { Color = TagColor.DarkGray };
            var tag2 = new StepTag("Another tag") { Color = TagColor.Red };

            var step1 = new SqlStep
            {
                StepName = "Test step 1",
                ExecutionPhase = 10,
                SqlStatement = "select 1",
                Connection = sqlConnection,
                Tags = [tag1, tag2]
            };

            var step2 = new SqlStep
            {
                StepName = "Test step 2",
                StepDescription = "Test step 2 description",
                ExecutionPhase = 20,
                SqlStatement = "select @param",
                Connection = sqlConnection,
                Tags = [tag1]
            };
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
                ParameterValueType = ParameterValueType.Int32,
                ValueInt32 = 10
            };
            step2.StepParameters.Add(step2Parameter);

            var step3 = new SqlStep
            {
                StepName = "Test step 3",
                ExecutionPhase = 20,
                SqlStatement = "select @param",
                Connection = sqlConnection,
                Tags = [tag1]
            };
            var step3Parameter = new SqlStepParameter
            {
                Step = step3,
                ParameterName = "@param",
                ParameterValueType = ParameterValueType.String,
                InheritFromJobParameter = jobParameter1
            };
            step3.StepParameters.Add(step3Parameter);

            var step4 = new SqlStep
            {
                StepName = "Test step 4",
                ExecutionPhase = 30,
                StepDescription = "Test step 4",
                SqlStatement = "select @param",
                Connection = sqlConnection,
                ExecutionConditionExpression = new() { Expression = "dt >= 2023" },
                Tags = [tag1]
            };
            var step4ExecConditionParam = new ExecutionConditionParameter
            {
                Step = step4,
                JobParameter = jobParameter2,
                ParameterValueType = ParameterValueType.DateTime,
                ParameterName = "dt"
            };
            step4.ExecutionConditionParameters.Add(step4ExecConditionParam);
            var step4Dependency = new Dependency
            {
                StepId = step4.StepId,
                Step = step4,
                DependantOnStepId = step3.StepId,
                DependantOnStep = step3,
                DependencyType = DependencyType.OnSucceeded
            };
            step4.Dependencies.Add(step4Dependency);
            var step4Parameter = new SqlStepParameter
            {
                Step = step4,
                ParameterName = "@param",
                ParameterValueType = ParameterValueType.String,
                UseExpression = true,
                Expression = new()
                {
                    Expression = """
                    $"{100 + 23}-{400 + 56}"
                    """
                }
            };
            step4.StepParameters.Add(step4Parameter);

            var step_1_5 = new SqlStep
            {
                StepName = "Test step 5",
                ExecutionPhase = 35,
                SqlStatement = "select @param",
                Connection = sqlConnection,
                Tags = []
            };
            var step_1_5_param = new SqlStepParameter
            {
                Step = step_1_5,
                ParameterName = "@param",
                ParameterValueType = ParameterValueType.String,
                InheritFromJobParameter = jobParameter4
            };
            step_1_5.StepParameters.Add(step_1_5_param);

            var step3Target = new DataObject
            {
                ObjectUri = DataObject.CreateTableUri("TestServer", "TestDb", "TestSchema", "TestTable"),
                MaxConcurrentWrites = 1
            };

            var dataObjectLink_3_3 = new StepDataObject { DataObject = step3Target, Step = step3, ReferenceType = DataObjectReferenceType.Target };
            var dataObjectLink_3_4 = new StepDataObject { DataObject = step3Target, Step = step4, ReferenceType = DataObjectReferenceType.Source };
            step3Target.Steps.AddRange([dataObjectLink_3_3, dataObjectLink_3_4]);

            step3.DataObjects.Add(dataObjectLink_3_3);

            var step4Target = new DataObject
            {
                ObjectUri = DataObject.CreateTableUri("TestServer", "TestDb", "TestSchema", "TestTable2"),
                MaxConcurrentWrites = 1
            };

            var dataObjectLink_4_4 = new StepDataObject { DataObject = step4Target, Step = step4, ReferenceType = DataObjectReferenceType.Target };
            step4Target.Steps.AddRange([dataObjectLink_4_4]);

            step4.DataObjects.Add(dataObjectLink_4_4);

            job1.Steps.AddRange([step1, step2, step3, step4, step_1_5]);
            #endregion

            #region JOB 2
            var job2 = new Job
            {
                JobName = "Test job 2",
                JobDescription = "Test job 2",
                ExecutionMode = ExecutionMode.ExecutionPhase,
                StopOnFirstError = true,
                MaxParallelSteps = 5,
                OvertimeNotificationLimitMinutes = 0
            };

            var step5 = new JobStep
            {
                StepName = "Test step 5",
                ExecutionPhase = 0,
                JobExecuteSynchronized = true,
                JobToExecute = job1,
                TagFilters = [tag2],
                Tags = []
            };

            var step6 = new JobStep
            {
                StepName = "Test step 6",
                ExecutionPhase = 0,
                JobExecuteSynchronized = true,
                JobToExecute = job1,
                TagFilters = [tag1, tag2],
                Tags = []
            };

            var step7 = new SqlStep
            {
                IsEnabled = false,
                StepName = "Test step 7",
                ExecutionPhase = 30,
                SqlStatement = "select 1",
                Connection = sqlConnection,
                Tags = [tag1]
            };

            var step8 = new SqlStep
            {
                StepName = "Test step 8",
                ExecutionPhase = 30,
                SqlStatement = "select 1",
                Connection = sqlConnection,
                Tags = [tag1]
            };

            var step9 = new PipelineStep
            {
                StepName = "Test step 9",
                ExecutionPhase = 35,
                PipelineClient = dataFactory,
                PipelineName = "test pipeline",
                Tags = []
            };

            var step10 = new PipelineStep
            {
                StepName = "Test step 10",
                ExecutionPhase = 40,
                PipelineClient = synapseWorkspace,
                PipelineName = "test pipeline 2",
                Tags = []
            };

            var step11 = new FunctionStep
            {
                StepName = "Test step 11",
                ExecutionPhase = 45,
                FunctionApp = functionApp,
                FunctionInput = "test-input",
                FunctionKey = "some-key",
                FunctionUrl = "http://function-url.com/test-function",
                Tags = []
            };

            var step12 = new QlikStep
            {
                StepName = "Test step 12",
                ExecutionPhase = 50,
                AppId = "some-app-id",
                QlikCloudClient = qlikClient,
                Tags = []
            };

            var step13 = new TabularStep
            {
                StepName = "Test step 13",
                ExecutionPhase = 55,
                Connection = asConnection
            };

            var step14 = new AgentJobStep
            {
                StepName = "Test step 14",
                ExecutionPhase = 60,
                Connection = sqlConnection
            };

            var step15 = new DatasetStep
            {
                StepName = "Test step 15",
                ExecutionPhase = 65,
                AppRegistration = appRegistration,
                DatasetGroupId = "some-workspace-id",
                DatasetId = "some-dataset-id"
            };

            var step16 = new EmailStep
            {
                StepName = "Test step 16",
                Recipients = "recipient@test.com",
                Subject = "This is a test email",
                Body = "This is a test email"
            };

            var step17 = new ExeStep
            {
                StepName = "Test step 17",
                ExeFileName = "dotnet",
                ExeArguments = "--version",
                ExeSuccessExitCode = 0,
                ExeWorkingDirectory = @"C:\"
            };

            var step18 = new PackageStep
            {
                StepName = "Test step 18",
                Connection = sqlConnection,
                PackageFolderName = "TestFolder",
                PackageProjectName = "TestProject",
                PackageName = "TestPackage.dtsx"
            };

            var step19 = new ExeStep
            {
                StepName = "Test step 19",
                ExeFileName = "dotnet",
                ExeArguments = "--version",
                ExeSuccessExitCode = 0,
                RunAsCredential = credential
            };

            job2.Steps.AddRange([step5, step6, step7, step8, step9, step10, step11, step12, step13, step14, step15, step16, step17, step18, step19]);
            #endregion

            #region SCHEDULES
            var schedule1 = new Schedule
            {
                JobId = job1.JobId,
                Job = job1,
                ScheduleName = "Test schedule 1",
                CronExpression = ""
            };
            var schedule2 = new Schedule
            {
                JobId = job2.JobId,
                Job = job2,
                ScheduleName = "Test schedule 2",
                CronExpression = ""
            };
            schedule2.Tags.Add(tag1);
            #endregion

            #region DATA TABLES

            var tableCategory = new MasterDataTableCategory { CategoryName = "Customer" };
            var table1 = new MasterDataTable
            {
                DataTableName = "Customer Groups",
                DataTableDescription = "Customer groups",
                TargetSchemaName = "dbo",
                TargetTableName = "CustomerGroup",
                Connection = sqlConnection,
                Category = tableCategory,
                ColumnOrder = ["Customer Group ID", "Customer Group Name"]
            };
            var table2 = new MasterDataTable
            {
                DataTableName = "Customers",
                DataTableDescription = "Customers",
                TargetSchemaName = "dbo",
                TargetTableName = "Customer",
                Connection = sqlConnection,
                Category = tableCategory,
                ColumnOrder = ["Customer ID", "Customer Name", "Customer Group"],
                HiddenColumns = ["Hidden Column"],
                LockedColumns = ["Locked Column 1", "Locked Column 2"]
            };
            var lookup = new MasterDataTableLookup
            {
                Table = table2,
                LookupTable = table1,
                ColumnName = "Customer Group",
                LookupValueColumn = "Customer Group ID",
                LookupDescriptionColumn = "Customer Group Name",
                LookupDisplayType = LookupDisplayType.ValueAndDescription
            };
            table2.Lookups.Add(lookup);
            #endregion

            context.AddRange(job1, job2, schedule1, schedule2, blobClient1, blobClient2, blobClient3, table1, table2, apiKey);
            await context.SaveChangesAsync();

            #region EXECUTIONS
            using var executionBuilder1 = await ExecutionBuilderFactory.CreateAsync(job1.JobId, Username);
            ArgumentNullException.ThrowIfNull(executionBuilder1);
            executionBuilder1.AddAll();
            await executionBuilder1.SaveExecutionAsync();

            using var executionBuilder2 = await ExecutionBuilderFactory.CreateAsync(job2.JobId, schedule1.ScheduleId, (ctx) => (step) => step.IsEnabled);
            ArgumentNullException.ThrowIfNull(executionBuilder2);
            executionBuilder2.AddAll();
            await executionBuilder2.SaveExecutionAsync();
            #endregion

            _databaseInitialized = true;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
