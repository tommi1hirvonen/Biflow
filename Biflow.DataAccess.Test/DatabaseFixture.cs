﻿using Biflow.Core;
using Biflow.Core.Entities;
using Biflow.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Biflow.DataAccess.Test;

public class DatabaseFixture : IAsyncLifetime
{
    private const string ConnectionString = 
        "Data Source=localhost;Database=BiflowTest;Integrated Security=sspi;Encrypt=true;TrustServerCertificate=true;";

    private static readonly SemaphoreSlim Semaphore = new(1, 1);
    private static bool _databaseInitialized;

    public static string Username => "testuser";

    private static string Role => "Admin";

    public IDbContextFactory<AppDbContext> DbContextFactory { get; }

    public IExecutionBuilderFactory<AppDbContext> ExecutionBuilderFactory { get; }

    public JobDuplicatorFactory JobDuplicatorFactory { get; }

    public StepsDuplicatorFactory StepsDuplicatorFactory { get; }

    public DatabaseFixture()
    {
        var userService = new MockUserService(Username, Role);   
        var settings = new Dictionary<string, string?>
        {
            { "ConnectionStrings:AppDbContext", ConnectionString }
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
        var jobDuplicatorFactory = services.GetRequiredService<JobDuplicatorFactory>();
        var stepsDuplicatorFactory = services.GetRequiredService<StepsDuplicatorFactory>();
        DbContextFactory = dbContextFactory;
        ExecutionBuilderFactory = executionBuilderFactory;
        JobDuplicatorFactory = jobDuplicatorFactory;
        StepsDuplicatorFactory = stepsDuplicatorFactory;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public async Task InitializeAsync()
    {
        try
        {
            await Semaphore.WaitAsync(); // Synchronize access

            if (_databaseInitialized)
            {
                return;
            }

            // Initialize seed data
            var context = await DbContextFactory.CreateDbContextAsync();
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();

            #region SETTINGS
            var sqlConnection = new MsSqlConnection
            {
                ConnectionName = "Test SQL connection",
                ConnectionString = ConnectionString
            };

            var asConnection = new AnalysisServicesConnection
            {
                ConnectionName = "Test AS connection",
                ConnectionString = "Data Source=localhost;Password=asd"
            };

            var azureCredential = new ServicePrincipalAzureCredential
            {
                AzureCredentialName = "Test credential",
                ClientId = "some-client-id",
                ClientSecret = "some-client-secret",
                TenantId = "some-tenant-id"
            };

            var dataFactory = new DataFactory
            {
                AzureCredential = azureCredential,
                PipelineClientName = "Test Data Factory",
                SubscriptionId = "some-subscription-id",
                ResourceGroupName = "some-resource-group-name",
                ResourceName = "some-resource-name"
            };

            var synapseWorkspace = new SynapseWorkspace
            {
                AzureCredential = azureCredential,
                PipelineClientName = "Test Synapse",
                SynapseWorkspaceUrl = "some-workspace-url"
            };

            var functionApp = new FunctionApp
            {
                AzureCredential = azureCredential,
                FunctionAppName = "Test function app",
                SubscriptionId = "some-subscription-id",
                ResourceGroupName = "some-resource-group-name",
                ResourceName = "some-resource-name",
                FunctionAppKey = "somefunctionappkey"
            };

            var qlikClient = new QlikCloudEnvironment
            {
                QlikCloudEnvironmentName = "Test Qlik Cloud Client",
                EnvironmentUrl = "https://test-qlik-url.com",
                ApiToken = "some-api-token"
            };

            var dbtAccount = new DbtAccount
            {
                DbtAccountName = "Test dbt account",
                ApiBaseUrl = "https://test-dbt.com",
                ApiToken = "some-api-key"
            };

            var databricksWorkspace = new DatabricksWorkspace
            {
                WorkspaceName = "Test databricks workspace",
                WorkspaceUrl = "https://test-databricks.com",
                ApiToken = "some-api-key"
            };

            var blobClient1 = new BlobStorageClient
            {
                BlobStorageClientName = "Test blob storage client"
            };
            blobClient1.UseCredential(azureCredential, "https://some-storage-account-url.com/");

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
            var jobTag1 = new JobTag("test-tag");
            var jobTag2 = new JobTag("second-tag");
            var job1 = new Job
            {
                JobName = "Test job 1",
                JobDescription = "Test job 1",
                ExecutionMode = ExecutionMode.Dependency,
                StopOnFirstError = true,
                MaxParallelSteps = 4,
                OvertimeNotificationLimitMinutes = 120
            };
            job1.Tags.AddRange([jobTag1, jobTag2]);
            var jobParameter1 = new JobParameter
            {
                Job = job1,
                ParameterName = "JobParameter1",
                ParameterValue = new ParameterValue("Hello world")
            };
            var jobParameter2 = new JobParameter
            {
                Job = job1,
                ParameterName = "JobParameter2",
                ParameterValue = new ParameterValue(DateTime.Now)
            };
            var jobParameter3 = new JobParameter
            {
                Job = job1,
                ParameterName = "JobParameter3",
                ParameterValue = new ParameterValue(400)
            };
            var jobParameter4 = new JobParameter
            {
                Job = job1,
                ParameterName = "JobParameter4",
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
                Connection = sqlConnection
            };
            step1.Tags.AddRange([tag1, tag2]);

            var step2 = new SqlStep
            {
                StepName = "Test step 2",
                StepDescription = "Test step 2 description",
                ExecutionPhase = 20,
                SqlStatement = "select @param",
                Connection = sqlConnection
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
                ParameterValue = new ParameterValue(10)
            };
            step2.StepParameters.Add(step2Parameter);

            var step3 = new SqlStep
            {
                StepName = "Test step 3",
                ExecutionPhase = 20,
                SqlStatement = "select @param",
                Connection = sqlConnection
            };
            step3.Tags.Add(tag1);
            var step3Parameter = new SqlStepParameter
            {
                Step = step3,
                ParameterName = "@param",
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
                ExecutionConditionExpression = new() { Expression = "dt >= 2023" }
            };
            step4.Tags.Add(tag1);
            var step4ExecConditionParam = new ExecutionConditionParameter
            {
                Step = step4,
                JobParameter = jobParameter2,
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
                UseExpression = true,
                Expression = new()
                {
                    Expression = """
                    $"{100 + 23}-{value + 56}"
                    """
                }
            };
            step4Parameter.AddExpressionParameter(jobParameter3);
            step4Parameter.ExpressionParameters.First().ParameterName = "value";
            step4.StepParameters.Add(step4Parameter);

            var step_1_5 = new SqlStep
            {
                StepName = "Test step 5",
                ExecutionPhase = 35,
                SqlStatement = "select @param",
                Connection = sqlConnection
            };
            var step_1_5_param = new SqlStepParameter
            {
                Step = step_1_5,
                ParameterName = "@param",
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
                JobToExecute = job1
            };
            step5.TagFilters.Add(tag2);

            var step6 = new JobStep
            {
                StepName = "Test step 6",
                ExecutionPhase = 0,
                JobExecuteSynchronized = true,
                JobToExecute = job1
            };
            step6.TagFilters.AddRange([tag1, tag2]);

            var step7 = new SqlStep
            {
                IsEnabled = false,
                StepName = "Test step 7",
                ExecutionPhase = 30,
                SqlStatement = "select 1",
                Connection = sqlConnection
            };
            step7.Tags.Add(tag1);

            var step8 = new SqlStep
            {
                StepName = "Test step 8",
                ExecutionPhase = 30,
                SqlStatement = "select 1",
                Connection = sqlConnection
            };
            step8.Tags.Add(tag1);

            var step9 = new PipelineStep
            {
                StepName = "Test step 9",
                ExecutionPhase = 35,
                PipelineClient = dataFactory,
                PipelineName = "test pipeline"
            };

            var step10 = new PipelineStep
            {
                StepName = "Test step 10",
                ExecutionPhase = 40,
                PipelineClient = synapseWorkspace,
                PipelineName = "test pipeline 2"
            };

            var step11 = new FunctionStep
            {
                StepName = "Test step 11",
                ExecutionPhase = 45,
                FunctionApp = functionApp,
                FunctionInput = "test-input",
                FunctionKey = "some-key",
                FunctionUrl = "https://function-url.com/test-function"
            };

            var step12 = new QlikStep
            {
                StepName = "Test step 12",
                ExecutionPhase = 50,
                QlikStepSettings = new QlikAppReloadSettings { AppId = "some-app-id"},
                QlikCloudEnvironment = qlikClient
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
                AzureCredential = azureCredential,
                WorkspaceId = "some-workspace-id",
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
            schedule2.TagFilter.Add(tag1);
            var scheduleTag = new ScheduleTag("schedule-tag");
            schedule2.Tags.Add(scheduleTag);
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

            context.AddRange(
                job1,
                job2,
                schedule1,
                schedule2,
                blobClient1,
                blobClient2,
                blobClient3,
                dbtAccount,
                databricksWorkspace,
                table1,
                table2,
                apiKey);
            await context.SaveChangesAsync();

            #region EXECUTIONS
            using var executionBuilder1 = await ExecutionBuilderFactory.CreateAsync(job1.JobId, Username);
            ArgumentNullException.ThrowIfNull(executionBuilder1);
            executionBuilder1.AddAll();
            await executionBuilder1.SaveExecutionAsync();

            using var executionBuilder2 = await ExecutionBuilderFactory.CreateAsync(
                job2.JobId,
                schedule1.ScheduleId,
                [_ => step => step.IsEnabled]);
            ArgumentNullException.ThrowIfNull(executionBuilder2);
            executionBuilder2.AddAll();
            await executionBuilder2.SaveExecutionAsync();
            #endregion

            _databaseInitialized = true;
        }
        finally
        {
            Semaphore.Release();
        }
    }
}
