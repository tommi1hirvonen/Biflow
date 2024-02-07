using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Biflow.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "app");

            migrationBuilder.CreateTable(
                name: "AppRegistration",
                schema: "app",
                columns: table => new
                {
                    AppRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppRegistrationName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    TenantId = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: false),
                    ClientId = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: false),
                    ClientSecret = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRegistration", x => x.AppRegistrationId);
                });

            migrationBuilder.CreateTable(
                name: "Connection",
                schema: "app",
                columns: table => new
                {
                    ConnectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConnectionType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    ConnectionName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ConnectionString = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExecutePackagesAsLogin = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Connection", x => x.ConnectionId);
                });

            migrationBuilder.CreateTable(
                name: "DataObject",
                schema: "app",
                columns: table => new
                {
                    ObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectUri = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false),
                    MaxConcurrentWrites = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataObject", x => x.ObjectId);
                });

            migrationBuilder.CreateTable(
                name: "DataTableCategory",
                schema: "app",
                columns: table => new
                {
                    DataTableCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataTableCategoryName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataTableCategory", x => x.DataTableCategoryId);
                });

            migrationBuilder.CreateTable(
                name: "EnvironmentVersion",
                schema: "app",
                columns: table => new
                {
                    VersionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Snapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnvironmentVersion", x => x.VersionId);
                });

            migrationBuilder.CreateTable(
                name: "Execution",
                schema: "app",
                columns: table => new
                {
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StartedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExecutionStatus = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    DependencyMode = table.Column<bool>(type: "bit", nullable: false),
                    StopOnFirstError = table.Column<bool>(type: "bit", nullable: false),
                    MaxParallelSteps = table.Column<int>(type: "int", nullable: false),
                    OvertimeNotificationLimitMinutes = table.Column<double>(type: "float", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScheduleName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CronExpression = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    ExecutorProcessId = table.Column<int>(type: "int", nullable: true),
                    Notify = table.Column<bool>(type: "bit", nullable: false),
                    NotifyCaller = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    NotifyCallerOvertime = table.Column<bool>(type: "bit", nullable: false),
                    ParentExecution = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Execution", x => x.ExecutionId);
                });

            migrationBuilder.CreateTable(
                name: "JobCategory",
                schema: "app",
                columns: table => new
                {
                    JobCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobCategoryName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobCategory", x => x.JobCategoryId);
                });

            migrationBuilder.CreateTable(
                name: "QlikCloudClient",
                schema: "app",
                columns: table => new
                {
                    QlikCloudClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QlikCloudClientName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    EnvironmentUrl = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ApiToken = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QlikCloudClient", x => x.QlikCloudClientId);
                });

            migrationBuilder.CreateTable(
                name: "Tag",
                schema: "app",
                columns: table => new
                {
                    TagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Color = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tag", x => x.TagId);
                });

            migrationBuilder.CreateTable(
                name: "User",
                schema: "app",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Email = table.Column<string>(type: "varchar(254)", unicode: false, maxLength: 254, nullable: true),
                    AuthorizeAllJobs = table.Column<bool>(type: "bit", nullable: false),
                    AuthorizeAllDataTables = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    PasswordHash = table.Column<string>(type: "varchar(100)", nullable: true),
                    Roles = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "AccessToken",
                schema: "app",
                columns: table => new
                {
                    AppRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceUrl = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: false),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiresOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessToken", x => new { x.AppRegistrationId, x.ResourceUrl });
                    table.ForeignKey(
                        name: "FK_AccessToken_AppRegistration_AppRegistrationId",
                        column: x => x.AppRegistrationId,
                        principalSchema: "app",
                        principalTable: "AppRegistration",
                        principalColumn: "AppRegistrationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BlobStorageClient",
                schema: "app",
                columns: table => new
                {
                    BlobStorageClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BlobStorageClientName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ConnectionMethod = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    StorageAccountUrl = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ConnectionString = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AppRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlobStorageClient", x => x.BlobStorageClientId);
                    table.ForeignKey(
                        name: "FK_BlobStorageClient_AppRegistration_AppRegistrationId",
                        column: x => x.AppRegistrationId,
                        principalSchema: "app",
                        principalTable: "AppRegistration",
                        principalColumn: "AppRegistrationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FunctionApp",
                schema: "app",
                columns: table => new
                {
                    FunctionAppId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FunctionAppName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    FunctionAppKey = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: true),
                    SubscriptionId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    ResourceGroupName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ResourceName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    AppRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FunctionApp", x => x.FunctionAppId);
                    table.ForeignKey(
                        name: "FK_FunctionApp_AppRegistration_AppRegistrationId",
                        column: x => x.AppRegistrationId,
                        principalSchema: "app",
                        principalTable: "AppRegistration",
                        principalColumn: "AppRegistrationId");
                });

            migrationBuilder.CreateTable(
                name: "PipelineClient",
                schema: "app",
                columns: table => new
                {
                    PipelineClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PipelineClientName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    PipelineClientType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    AppRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriptionId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    ResourceGroupName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ResourceName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    SynapseWorkspaceUrl = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PipelineClient", x => x.PipelineClientId);
                    table.ForeignKey(
                        name: "FK_PipelineClient_AppRegistration_AppRegistrationId",
                        column: x => x.AppRegistrationId,
                        principalSchema: "app",
                        principalTable: "AppRegistration",
                        principalColumn: "AppRegistrationId");
                });

            migrationBuilder.CreateTable(
                name: "DataTable",
                schema: "app",
                columns: table => new
                {
                    DataTableId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataTableName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    DataTableDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetSchemaName = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    TargetTableName = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataTableCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AllowInsert = table.Column<bool>(type: "bit", nullable: false),
                    AllowDelete = table.Column<bool>(type: "bit", nullable: false),
                    AllowUpdate = table.Column<bool>(type: "bit", nullable: false),
                    AllowImport = table.Column<bool>(type: "bit", nullable: false),
                    LockedColumns = table.Column<string>(type: "varchar(8000)", unicode: false, maxLength: 8000, nullable: false),
                    LockedColumnsExcludeMode = table.Column<bool>(type: "bit", nullable: false),
                    HiddenColumns = table.Column<string>(type: "varchar(8000)", unicode: false, maxLength: 8000, nullable: false),
                    ColumnOrder = table.Column<string>(type: "varchar(8000)", unicode: false, maxLength: 8000, nullable: false),
                    Timestamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataTable", x => x.DataTableId);
                    table.ForeignKey(
                        name: "FK_DataTable_Connection_ConnectionId",
                        column: x => x.ConnectionId,
                        principalSchema: "app",
                        principalTable: "Connection",
                        principalColumn: "ConnectionId");
                    table.ForeignKey(
                        name: "FK_DataTable_DataTableCategory_DataTableCategoryId",
                        column: x => x.DataTableCategoryId,
                        principalSchema: "app",
                        principalTable: "DataTableCategory",
                        principalColumn: "DataTableCategoryId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionConcurrency",
                schema: "app",
                columns: table => new
                {
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    MaxParallelSteps = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionConcurrency", x => new { x.ExecutionId, x.StepType });
                    table.ForeignKey(
                        name: "FK_ExecutionConcurrency_Execution_ExecutionId",
                        column: x => x.ExecutionId,
                        principalSchema: "app",
                        principalTable: "Execution",
                        principalColumn: "ExecutionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionDataObject",
                schema: "app",
                columns: table => new
                {
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectUri = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false),
                    MaxConcurrentWrites = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionDataObject", x => new { x.ExecutionId, x.ObjectId });
                    table.ForeignKey(
                        name: "FK_ExecutionDataObject_Execution_ExecutionId",
                        column: x => x.ExecutionId,
                        principalSchema: "app",
                        principalTable: "Execution",
                        principalColumn: "ExecutionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionParameter",
                schema: "app",
                columns: table => new
                {
                    ParameterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefaultValue = table.Column<object>(type: "sql_variant", nullable: true),
                    ParameterName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ParameterValue = table.Column<object>(type: "sql_variant", nullable: true),
                    ParameterValueType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    UseExpression = table.Column<bool>(type: "bit", nullable: false),
                    Expression = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionParameter", x => new { x.ExecutionId, x.ParameterId });
                    table.ForeignKey(
                        name: "FK_ExecutionParameter_Execution_ExecutionId",
                        column: x => x.ExecutionId,
                        principalSchema: "app",
                        principalTable: "Execution",
                        principalColumn: "ExecutionId");
                });

            migrationBuilder.CreateTable(
                name: "Job",
                schema: "app",
                columns: table => new
                {
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    JobDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UseDependencyMode = table.Column<bool>(type: "bit", nullable: false),
                    StopOnFirstError = table.Column<bool>(type: "bit", nullable: false),
                    MaxParallelSteps = table.Column<int>(type: "int", nullable: false),
                    OvertimeNotificationLimitMinutes = table.Column<double>(type: "float", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    JobCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Timestamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Job", x => x.JobId);
                    table.ForeignKey(
                        name: "FK_Job_JobCategory_JobCategoryId",
                        column: x => x.JobCategoryId,
                        principalSchema: "app",
                        principalTable: "JobCategory",
                        principalColumn: "JobCategoryId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DataTableAuthorization",
                schema: "app",
                columns: table => new
                {
                    DataTableId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataTableAuthorization", x => new { x.DataTableId, x.UserId });
                    table.ForeignKey(
                        name: "FK_DataTableAuthorization_DataTable_DataTableId",
                        column: x => x.DataTableId,
                        principalSchema: "app",
                        principalTable: "DataTable",
                        principalColumn: "DataTableId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DataTableAuthorization_User_UserId",
                        column: x => x.UserId,
                        principalSchema: "app",
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DataTableLookup",
                schema: "app",
                columns: table => new
                {
                    LookupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataTableId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ColumnName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LookupDataTableId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LookupValueColumn = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LookupDescriptionColumn = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LookupDisplayType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataTableLookup", x => x.LookupId);
                    table.ForeignKey(
                        name: "FK_DataTableLookup_DataTable_DataTableId",
                        column: x => x.DataTableId,
                        principalSchema: "app",
                        principalTable: "DataTable",
                        principalColumn: "DataTableId");
                    table.ForeignKey(
                        name: "FK_DataTableLookup_DataTable_LookupDataTableId",
                        column: x => x.LookupDataTableId,
                        principalSchema: "app",
                        principalTable: "DataTable",
                        principalColumn: "DataTableId");
                });

            migrationBuilder.CreateTable(
                name: "ExecutionStep",
                schema: "app",
                columns: table => new
                {
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    StepType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    DuplicateExecutionBehaviour = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    ExecutionPhase = table.Column<int>(type: "int", nullable: false),
                    RetryAttempts = table.Column<int>(type: "int", nullable: false),
                    RetryIntervalMinutes = table.Column<double>(type: "float", nullable: false),
                    ExecutionConditionExpression = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AgentJobName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    TimeoutMinutes = table.Column<double>(type: "float", nullable: true),
                    ConnectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AppRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DatasetGroupId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    DatasetId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    EmailRecipients = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailSubject = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExeFileName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ExeArguments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExeWorkingDirectory = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ExeSuccessExitCode = table.Column<int>(type: "int", nullable: true),
                    FunctionAppId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FunctionUrl = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: true),
                    FunctionInput = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FunctionIsDurable = table.Column<bool>(type: "bit", nullable: true),
                    JobToExecuteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JobExecuteSynchronized = table.Column<bool>(type: "bit", nullable: true),
                    TagFilters = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PackageFolderName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PackageProjectName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PackageName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    ExecuteIn32BitMode = table.Column<bool>(type: "bit", nullable: true),
                    ExecuteAsLogin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PipelineName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    PipelineClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AppId = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: true),
                    QlikCloudClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SqlStatement = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResultCaptureJobParameterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResultCaptureJobParameterValue = table.Column<object>(type: "sql_variant", nullable: true),
                    TabularModelName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    TabularTableName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    TabularPartitionName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionStep", x => new { x.ExecutionId, x.StepId });
                    table.ForeignKey(
                        name: "FK_ExecutionStep_ExecutionParameter_ExecutionId_ResultCaptureJobParameterId",
                        columns: x => new { x.ExecutionId, x.ResultCaptureJobParameterId },
                        principalSchema: "app",
                        principalTable: "ExecutionParameter",
                        principalColumns: new[] { "ExecutionId", "ParameterId" });
                    table.ForeignKey(
                        name: "FK_ExecutionStep_Execution_ExecutionId",
                        column: x => x.ExecutionId,
                        principalSchema: "app",
                        principalTable: "Execution",
                        principalColumn: "ExecutionId");
                });

            migrationBuilder.CreateTable(
                name: "JobAuthorization",
                schema: "app",
                columns: table => new
                {
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobAuthorization", x => new { x.JobId, x.UserId });
                    table.ForeignKey(
                        name: "FK_JobAuthorization_Job_JobId",
                        column: x => x.JobId,
                        principalSchema: "app",
                        principalTable: "Job",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobAuthorization_User_UserId",
                        column: x => x.UserId,
                        principalSchema: "app",
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobConcurrency",
                schema: "app",
                columns: table => new
                {
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    MaxParallelSteps = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobConcurrency", x => new { x.JobId, x.StepType });
                    table.ForeignKey(
                        name: "FK_JobConcurrency_Job_JobId",
                        column: x => x.JobId,
                        principalSchema: "app",
                        principalTable: "Job",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobParameter",
                schema: "app",
                columns: table => new
                {
                    ParameterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParameterName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ParameterValue = table.Column<object>(type: "sql_variant", nullable: true),
                    ParameterValueType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    UseExpression = table.Column<bool>(type: "bit", nullable: false),
                    Expression = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobParameter", x => x.ParameterId);
                    table.ForeignKey(
                        name: "FK_JobParameter_Job_JobId",
                        column: x => x.JobId,
                        principalSchema: "app",
                        principalTable: "Job",
                        principalColumn: "JobId");
                });

            migrationBuilder.CreateTable(
                name: "Schedule",
                schema: "app",
                columns: table => new
                {
                    ScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduleName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    CronExpression = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    DisallowConcurrentExecution = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedule", x => x.ScheduleId);
                    table.ForeignKey(
                        name: "FK_Schedule_Job_JobId",
                        column: x => x.JobId,
                        principalSchema: "app",
                        principalTable: "Job",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionDependency",
                schema: "app",
                columns: table => new
                {
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DependantOnStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DependencyType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionDependency", x => new { x.ExecutionId, x.StepId, x.DependantOnStepId });
                    table.CheckConstraint("CK_ExecutionDependency", "[StepId]<>[DependantOnStepId]");
                    table.ForeignKey(
                        name: "FK_ExecutionDependency_ExecutionStep_ExecutionId_DependantOnStepId",
                        columns: x => new { x.ExecutionId, x.DependantOnStepId },
                        principalSchema: "app",
                        principalTable: "ExecutionStep",
                        principalColumns: new[] { "ExecutionId", "StepId" });
                    table.ForeignKey(
                        name: "FK_ExecutionDependency_ExecutionStep_ExecutionId_StepId",
                        columns: x => new { x.ExecutionId, x.StepId },
                        principalSchema: "app",
                        principalTable: "ExecutionStep",
                        principalColumns: new[] { "ExecutionId", "StepId" });
                });

            migrationBuilder.CreateTable(
                name: "ExecutionStepAttempt",
                schema: "app",
                columns: table => new
                {
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RetryAttemptIndex = table.Column<int>(type: "int", nullable: false),
                    StartedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExecutionStatus = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    StepType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    StoppedBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ErrorMessages = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InfoMessages = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WarningMessages = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExeProcessId = table.Column<int>(type: "int", nullable: true),
                    FunctionInstanceId = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ChildJobExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PackageOperationId = table.Column<long>(type: "bigint", nullable: true),
                    PipelineRunId = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ReloadId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionStepAttempt", x => new { x.ExecutionId, x.StepId, x.RetryAttemptIndex });
                    table.ForeignKey(
                        name: "FK_ExecutionStepAttempt_ExecutionStep_ExecutionId_StepId",
                        columns: x => new { x.ExecutionId, x.StepId },
                        principalSchema: "app",
                        principalTable: "ExecutionStep",
                        principalColumns: new[] { "ExecutionId", "StepId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionStepConditionParameter",
                schema: "app",
                columns: table => new
                {
                    ParameterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParameterValueType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    ExecutionParameterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExecutionParameterValue = table.Column<object>(type: "sql_variant", nullable: true),
                    ParameterName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ParameterValue = table.Column<object>(type: "sql_variant", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionStepConditionParameter", x => new { x.ExecutionId, x.ParameterId });
                    table.ForeignKey(
                        name: "FK_ExecutionStepConditionParameter_ExecutionParameter_ExecutionId_ExecutionParameterId",
                        columns: x => new { x.ExecutionId, x.ExecutionParameterId },
                        principalSchema: "app",
                        principalTable: "ExecutionParameter",
                        principalColumns: new[] { "ExecutionId", "ParameterId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExecutionStepConditionParameter_ExecutionStep_ExecutionId_StepId",
                        columns: x => new { x.ExecutionId, x.StepId },
                        principalSchema: "app",
                        principalTable: "ExecutionStep",
                        principalColumns: new[] { "ExecutionId", "StepId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionStepDataObject",
                schema: "app",
                columns: table => new
                {
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferenceType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    DataAttributes = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionStepDataObject", x => new { x.ExecutionId, x.StepId, x.ObjectId });
                    table.ForeignKey(
                        name: "FK_ExecutionStepDataObject_ExecutionDataObject_ExecutionId_ObjectId",
                        columns: x => new { x.ExecutionId, x.ObjectId },
                        principalSchema: "app",
                        principalTable: "ExecutionDataObject",
                        principalColumns: new[] { "ExecutionId", "ObjectId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExecutionStepDataObject_ExecutionStep_ExecutionId_StepId",
                        columns: x => new { x.ExecutionId, x.StepId },
                        principalSchema: "app",
                        principalTable: "ExecutionStep",
                        principalColumns: new[] { "ExecutionId", "StepId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionStepParameter",
                schema: "app",
                columns: table => new
                {
                    ParameterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParameterType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    ParameterValueType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    InheritFromExecutionParameterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExecutionParameterValue = table.Column<object>(type: "sql_variant", nullable: true),
                    UseExpression = table.Column<bool>(type: "bit", nullable: false),
                    AssignToJobParameterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParameterLevel = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    ParameterName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ParameterValue = table.Column<object>(type: "sql_variant", nullable: true),
                    Expression = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionStepParameter", x => new { x.ExecutionId, x.ParameterId });
                    table.ForeignKey(
                        name: "FK_ExecutionStepParameter_ExecutionParameter_ExecutionId_InheritFromExecutionParameterId",
                        columns: x => new { x.ExecutionId, x.InheritFromExecutionParameterId },
                        principalSchema: "app",
                        principalTable: "ExecutionParameter",
                        principalColumns: new[] { "ExecutionId", "ParameterId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExecutionStepParameter_ExecutionStep_ExecutionId_StepId",
                        columns: x => new { x.ExecutionId, x.StepId },
                        principalSchema: "app",
                        principalTable: "ExecutionStep",
                        principalColumns: new[] { "ExecutionId", "StepId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Step",
                schema: "app",
                columns: table => new
                {
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    StepDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExecutionPhase = table.Column<int>(type: "int", nullable: false),
                    StepType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    DuplicateExecutionBehaviour = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    RetryAttempts = table.Column<int>(type: "int", nullable: false),
                    RetryIntervalMinutes = table.Column<double>(type: "float", nullable: false),
                    ExecutionConditionExpression = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Timestamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    TimeoutMinutes = table.Column<double>(type: "float", nullable: true),
                    AgentJobName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ConnectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AppRegistrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DatasetGroupId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    DatasetId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    EmailRecipients = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailSubject = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExeFileName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ExeArguments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExeWorkingDirectory = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ExeSuccessExitCode = table.Column<int>(type: "int", nullable: true),
                    FunctionAppId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FunctionUrl = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: true),
                    FunctionInput = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FunctionIsDurable = table.Column<bool>(type: "bit", nullable: true),
                    FunctionKey = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    JobToExecuteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JobExecuteSynchronized = table.Column<bool>(type: "bit", nullable: true),
                    PackageFolderName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PackageProjectName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PackageName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    ExecuteIn32BitMode = table.Column<bool>(type: "bit", nullable: true),
                    ExecuteAsLogin = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PipelineClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PipelineName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    AppId = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: true),
                    QlikCloudClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SqlStatement = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResultCaptureJobParameterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TabularModelName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    TabularTableName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    TabularPartitionName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Step", x => x.StepId);
                    table.ForeignKey(
                        name: "FK_Step_AppRegistration_AppRegistrationId",
                        column: x => x.AppRegistrationId,
                        principalSchema: "app",
                        principalTable: "AppRegistration",
                        principalColumn: "AppRegistrationId");
                    table.ForeignKey(
                        name: "FK_Step_Connection_ConnectionId",
                        column: x => x.ConnectionId,
                        principalSchema: "app",
                        principalTable: "Connection",
                        principalColumn: "ConnectionId");
                    table.ForeignKey(
                        name: "FK_Step_FunctionApp_FunctionAppId",
                        column: x => x.FunctionAppId,
                        principalSchema: "app",
                        principalTable: "FunctionApp",
                        principalColumn: "FunctionAppId");
                    table.ForeignKey(
                        name: "FK_Step_JobParameter_ResultCaptureJobParameterId",
                        column: x => x.ResultCaptureJobParameterId,
                        principalSchema: "app",
                        principalTable: "JobParameter",
                        principalColumn: "ParameterId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Step_Job_JobId",
                        column: x => x.JobId,
                        principalSchema: "app",
                        principalTable: "Job",
                        principalColumn: "JobId");
                    table.ForeignKey(
                        name: "FK_Step_Job_JobToExecuteId",
                        column: x => x.JobToExecuteId,
                        principalSchema: "app",
                        principalTable: "Job",
                        principalColumn: "JobId");
                    table.ForeignKey(
                        name: "FK_Step_PipelineClient_PipelineClientId",
                        column: x => x.PipelineClientId,
                        principalSchema: "app",
                        principalTable: "PipelineClient",
                        principalColumn: "PipelineClientId");
                    table.ForeignKey(
                        name: "FK_Step_QlikCloudClient_QlikCloudClientId",
                        column: x => x.QlikCloudClientId,
                        principalSchema: "app",
                        principalTable: "QlikCloudClient",
                        principalColumn: "QlikCloudClientId");
                });

            migrationBuilder.CreateTable(
                name: "ScheduleTag",
                schema: "app",
                columns: table => new
                {
                    ScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleTag", x => new { x.ScheduleId, x.TagId });
                    table.ForeignKey(
                        name: "FK_ScheduleTag_Schedule_ScheduleId",
                        column: x => x.ScheduleId,
                        principalSchema: "app",
                        principalTable: "Schedule",
                        principalColumn: "ScheduleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScheduleTag_Tag_TagId",
                        column: x => x.TagId,
                        principalSchema: "app",
                        principalTable: "Tag",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionStepParameterExpressionParameter",
                schema: "app",
                columns: table => new
                {
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParameterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepParameterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParameterName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    InheritFromExecutionParameterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionStepParameterExpressionParameter", x => new { x.ExecutionId, x.ParameterId });
                    table.ForeignKey(
                        name: "FK_ExecutionStepParameterExpressionParameter_ExecutionParameter_ExecutionId_InheritFromExecutionParameterId",
                        columns: x => new { x.ExecutionId, x.InheritFromExecutionParameterId },
                        principalSchema: "app",
                        principalTable: "ExecutionParameter",
                        principalColumns: new[] { "ExecutionId", "ParameterId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExecutionStepParameterExpressionParameter_ExecutionStepParameter_ExecutionId_StepParameterId",
                        columns: x => new { x.ExecutionId, x.StepParameterId },
                        principalSchema: "app",
                        principalTable: "ExecutionStepParameter",
                        principalColumns: new[] { "ExecutionId", "ParameterId" });
                });

            migrationBuilder.CreateTable(
                name: "Dependency",
                schema: "app",
                columns: table => new
                {
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DependantOnStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DependencyType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dependency", x => new { x.StepId, x.DependantOnStepId });
                    table.CheckConstraint("CK_Dependency", "[StepId]<>[DependantOnStepId]");
                    table.ForeignKey(
                        name: "FK_Dependency_Step_DependantOnStepId",
                        column: x => x.DependantOnStepId,
                        principalSchema: "app",
                        principalTable: "Step",
                        principalColumn: "StepId");
                    table.ForeignKey(
                        name: "FK_Dependency_Step_StepId",
                        column: x => x.StepId,
                        principalSchema: "app",
                        principalTable: "Step",
                        principalColumn: "StepId");
                });

            migrationBuilder.CreateTable(
                name: "JobStepTagFilter",
                schema: "app",
                columns: table => new
                {
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobStepTagFilter", x => new { x.StepId, x.TagId });
                    table.ForeignKey(
                        name: "FK_JobStepTagFilter_Step_StepId",
                        column: x => x.StepId,
                        principalSchema: "app",
                        principalTable: "Step",
                        principalColumn: "StepId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobStepTagFilter_Tag_TagId",
                        column: x => x.TagId,
                        principalSchema: "app",
                        principalTable: "Tag",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StepConditionParameter",
                schema: "app",
                columns: table => new
                {
                    ParameterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobParameterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParameterName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ParameterValue = table.Column<object>(type: "sql_variant", nullable: true),
                    ParameterValueType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StepConditionParameter", x => x.ParameterId);
                    table.ForeignKey(
                        name: "FK_StepConditionParameter_JobParameter_JobParameterId",
                        column: x => x.JobParameterId,
                        principalSchema: "app",
                        principalTable: "JobParameter",
                        principalColumn: "ParameterId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StepConditionParameter_Step_StepId",
                        column: x => x.StepId,
                        principalSchema: "app",
                        principalTable: "Step",
                        principalColumn: "StepId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StepDataObject",
                schema: "app",
                columns: table => new
                {
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferenceType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    DataAttributes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StepDataObject", x => new { x.StepId, x.ObjectId, x.ReferenceType });
                    table.ForeignKey(
                        name: "FK_StepDataObject_DataObject_ObjectId",
                        column: x => x.ObjectId,
                        principalSchema: "app",
                        principalTable: "DataObject",
                        principalColumn: "ObjectId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StepDataObject_Step_StepId",
                        column: x => x.StepId,
                        principalSchema: "app",
                        principalTable: "Step",
                        principalColumn: "StepId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StepParameter",
                schema: "app",
                columns: table => new
                {
                    ParameterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParameterType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    InheritFromJobParameterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignToJobParameterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParameterLevel = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    ParameterName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ParameterValue = table.Column<object>(type: "sql_variant", nullable: true),
                    ParameterValueType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    UseExpression = table.Column<bool>(type: "bit", nullable: false),
                    Expression = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StepParameter", x => x.ParameterId);
                    table.ForeignKey(
                        name: "FK_StepParameter_JobParameter_AssignToJobParameterId",
                        column: x => x.AssignToJobParameterId,
                        principalSchema: "app",
                        principalTable: "JobParameter",
                        principalColumn: "ParameterId");
                    table.ForeignKey(
                        name: "FK_StepParameter_JobParameter_InheritFromJobParameterId",
                        column: x => x.InheritFromJobParameterId,
                        principalSchema: "app",
                        principalTable: "JobParameter",
                        principalColumn: "ParameterId");
                    table.ForeignKey(
                        name: "FK_StepParameter_Step_StepId",
                        column: x => x.StepId,
                        principalSchema: "app",
                        principalTable: "Step",
                        principalColumn: "StepId");
                });

            migrationBuilder.CreateTable(
                name: "StepTag",
                schema: "app",
                columns: table => new
                {
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StepTag", x => new { x.StepId, x.TagId });
                    table.ForeignKey(
                        name: "FK_StepTag_Step_StepId",
                        column: x => x.StepId,
                        principalSchema: "app",
                        principalTable: "Step",
                        principalColumn: "StepId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StepTag_Tag_TagId",
                        column: x => x.TagId,
                        principalSchema: "app",
                        principalTable: "Tag",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subscription",
                schema: "app",
                columns: table => new
                {
                    SubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriptionType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlertType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NotifyOnOvertime = table.Column<bool>(type: "bit", nullable: true),
                    TagId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscription", x => x.SubscriptionId);
                    table.ForeignKey(
                        name: "FK_Subscription_Job_JobId",
                        column: x => x.JobId,
                        principalSchema: "app",
                        principalTable: "Job",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subscription_Step_StepId",
                        column: x => x.StepId,
                        principalSchema: "app",
                        principalTable: "Step",
                        principalColumn: "StepId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subscription_Tag_TagId",
                        column: x => x.TagId,
                        principalSchema: "app",
                        principalTable: "Tag",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subscription_User_UserId",
                        column: x => x.UserId,
                        principalSchema: "app",
                        principalTable: "User",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StepParameterExpressionParameter",
                schema: "app",
                columns: table => new
                {
                    ParameterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepParameterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParameterName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    InheritFromJobParameterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StepParameterExpressionParameter", x => x.ParameterId);
                    table.ForeignKey(
                        name: "FK_StepParameterExpressionParameter_JobParameter_InheritFromJobParameterId",
                        column: x => x.InheritFromJobParameterId,
                        principalSchema: "app",
                        principalTable: "JobParameter",
                        principalColumn: "ParameterId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StepParameterExpressionParameter_StepParameter_StepParameterId",
                        column: x => x.StepParameterId,
                        principalSchema: "app",
                        principalTable: "StepParameter",
                        principalColumn: "ParameterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_DataObject",
                schema: "app",
                table: "DataObject",
                column: "ObjectUri",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_DataTableCategory",
                schema: "app",
                table: "DataTableCategory",
                column: "DataTableCategoryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_DataTableLookup",
                schema: "app",
                table: "DataTableLookup",
                columns: new[] { "DataTableId", "ColumnName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Execution_CreatedOn_EndedOn",
                schema: "app",
                table: "Execution",
                columns: new[] { "CreatedOn", "EndedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Execution_ExecutionStatus",
                schema: "app",
                table: "Execution",
                column: "ExecutionStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Execution_JobId_CreatedOn",
                schema: "app",
                table: "Execution",
                columns: new[] { "JobId", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "UQ_ExecutionStepParameterExpressionParameter",
                schema: "app",
                table: "ExecutionStepParameterExpressionParameter",
                columns: new[] { "ExecutionId", "StepParameterId", "ParameterName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_JobCategory",
                schema: "app",
                table: "JobCategory",
                column: "JobCategoryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_JobParameter",
                schema: "app",
                table: "JobParameter",
                columns: new[] { "JobId", "ParameterName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Schedule",
                schema: "app",
                table: "Schedule",
                columns: new[] { "JobId", "CronExpression" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_StepConditionParameter",
                schema: "app",
                table: "StepConditionParameter",
                columns: new[] { "StepId", "ParameterName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_StepParameter",
                schema: "app",
                table: "StepParameter",
                columns: new[] { "StepId", "ParameterLevel", "ParameterName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_StepParameterExpressionParameter",
                schema: "app",
                table: "StepParameterExpressionParameter",
                columns: new[] { "StepParameterId", "ParameterName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UQ_Subscription_JobSubscription",
                schema: "app",
                table: "Subscription",
                columns: new[] { "UserId", "JobId" },
                unique: true,
                filter: "[SubscriptionType] = 'Job'");

            migrationBuilder.CreateIndex(
                name: "IX_UQ_Subscription_JobTagSubscription",
                schema: "app",
                table: "Subscription",
                columns: new[] { "UserId", "JobId", "TagId" },
                unique: true,
                filter: "[SubscriptionType] = 'JobTag'");

            migrationBuilder.CreateIndex(
                name: "IX_UQ_Subscription_StepSubscription",
                schema: "app",
                table: "Subscription",
                columns: new[] { "UserId", "StepId" },
                unique: true,
                filter: "[SubscriptionType] = 'Step'");

            migrationBuilder.CreateIndex(
                name: "IX_UQ_Subscription_TagSubscription",
                schema: "app",
                table: "Subscription",
                columns: new[] { "UserId", "TagId" },
                unique: true,
                filter: "[SubscriptionType] = 'Tag'");

            migrationBuilder.CreateIndex(
                name: "UQ_TagName",
                schema: "app",
                table: "Tag",
                column: "TagName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_User",
                schema: "app",
                table: "User",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessToken",
                schema: "app");

            migrationBuilder.DropTable(
                name: "BlobStorageClient",
                schema: "app");

            migrationBuilder.DropTable(
                name: "DataTableAuthorization",
                schema: "app");

            migrationBuilder.DropTable(
                name: "DataTableLookup",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Dependency",
                schema: "app");

            migrationBuilder.DropTable(
                name: "EnvironmentVersion",
                schema: "app");

            migrationBuilder.DropTable(
                name: "ExecutionConcurrency",
                schema: "app");

            migrationBuilder.DropTable(
                name: "ExecutionDependency",
                schema: "app");

            migrationBuilder.DropTable(
                name: "ExecutionStepAttempt",
                schema: "app");

            migrationBuilder.DropTable(
                name: "ExecutionStepConditionParameter",
                schema: "app");

            migrationBuilder.DropTable(
                name: "ExecutionStepDataObject",
                schema: "app");

            migrationBuilder.DropTable(
                name: "ExecutionStepParameterExpressionParameter",
                schema: "app");

            migrationBuilder.DropTable(
                name: "JobAuthorization",
                schema: "app");

            migrationBuilder.DropTable(
                name: "JobConcurrency",
                schema: "app");

            migrationBuilder.DropTable(
                name: "JobStepTagFilter",
                schema: "app");

            migrationBuilder.DropTable(
                name: "ScheduleTag",
                schema: "app");

            migrationBuilder.DropTable(
                name: "StepConditionParameter",
                schema: "app");

            migrationBuilder.DropTable(
                name: "StepDataObject",
                schema: "app");

            migrationBuilder.DropTable(
                name: "StepParameterExpressionParameter",
                schema: "app");

            migrationBuilder.DropTable(
                name: "StepTag",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Subscription",
                schema: "app");

            migrationBuilder.DropTable(
                name: "DataTable",
                schema: "app");

            migrationBuilder.DropTable(
                name: "ExecutionDataObject",
                schema: "app");

            migrationBuilder.DropTable(
                name: "ExecutionStepParameter",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Schedule",
                schema: "app");

            migrationBuilder.DropTable(
                name: "DataObject",
                schema: "app");

            migrationBuilder.DropTable(
                name: "StepParameter",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Tag",
                schema: "app");

            migrationBuilder.DropTable(
                name: "User",
                schema: "app");

            migrationBuilder.DropTable(
                name: "DataTableCategory",
                schema: "app");

            migrationBuilder.DropTable(
                name: "ExecutionStep",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Step",
                schema: "app");

            migrationBuilder.DropTable(
                name: "ExecutionParameter",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Connection",
                schema: "app");

            migrationBuilder.DropTable(
                name: "FunctionApp",
                schema: "app");

            migrationBuilder.DropTable(
                name: "JobParameter",
                schema: "app");

            migrationBuilder.DropTable(
                name: "PipelineClient",
                schema: "app");

            migrationBuilder.DropTable(
                name: "QlikCloudClient",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Execution",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Job",
                schema: "app");

            migrationBuilder.DropTable(
                name: "AppRegistration",
                schema: "app");

            migrationBuilder.DropTable(
                name: "JobCategory",
                schema: "app");
        }
    }
}
