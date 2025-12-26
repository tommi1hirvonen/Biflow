using Biflow.DataAccess.Configuration;
using Biflow.DataAccess.Convention;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.Configuration;

namespace Biflow.DataAccess;

public class AppDbContext : DbContext
{
    private readonly string _connectionString;
    private readonly IUserService? _userService;

    public AppDbContext(IConfiguration configuration, IUserService? userService = null)
    {
        _connectionString = configuration.GetConnectionString("AppDbContext")
            ?? throw new ApplicationException("Connection string not found");
        _userService = userService;

        SavingChanges += OnSavingChanges;
    }

    internal string? Username => _userService?.Username;

    internal IEnumerable<string>? UserRoles => _userService?.Roles;

    #region DbSets
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<Step> Steps => Set<Step>();
    public DbSet<DatasetStep> DatasetSteps => Set<DatasetStep>();
    public DbSet<DataflowStep> DataflowSteps => Set<DataflowStep>();
    public DbSet<FabricStep> FabricSteps => Set<FabricStep>();
    public DbSet<ExeStep> ExeSteps => Set<ExeStep>();
    public DbSet<JobStep> JobSteps => Set<JobStep>();
    public DbSet<PackageStep> PackageSteps => Set<PackageStep>();
    public DbSet<PipelineStep> PipelineSteps => Set<PipelineStep>();
    public DbSet<SqlStep> SqlSteps => Set<SqlStep>();
    public DbSet<FunctionStep> FunctionSteps => Set<FunctionStep>();
    public DbSet<AgentJobStep> AgentJobSteps => Set<AgentJobStep>();
    public DbSet<TabularStep> TabularSteps => Set<TabularStep>();
    public DbSet<EmailStep> EmailSteps => Set<EmailStep>();
    public DbSet<QlikStep> QlikSteps => Set<QlikStep>();
    public DbSet<DatabricksStep> DatabricksSteps => Set<DatabricksStep>();
    public DbSet<DbtStep> DbtSteps => Set<DbtStep>();
    public DbSet<ScdStep> ScdSteps => Set<ScdStep>();
    public DbSet<HttpStep> HttpSteps => Set<HttpStep>();
    public DbSet<DataObject> DataObjects => Set<DataObject>();
    public DbSet<Execution> Executions => Set<Execution>();
    public DbSet<StepExecution> StepExecutions => Set<StepExecution>();
    public DbSet<StepExecutionAttempt> StepExecutionAttempts => Set<StepExecutionAttempt>();
    public DbSet<StepExecutionMonitor> StepExecutionMonitors => Set<StepExecutionMonitor>();
    public DbSet<StepExecutionParameterExpressionParameter> StepExecutionParameterExpressionParameters => Set<StepExecutionParameterExpressionParameter>();
    public DbSet<Dependency> Dependencies => Set<Dependency>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<JobSubscription> JobSubscriptions => Set<JobSubscription>();
    public DbSet<JobStepTagSubscription> JobStepTagSubscriptions => Set<JobStepTagSubscription>();
    public DbSet<StepSubscription> StepSubscriptions => Set<StepSubscription>();
    public DbSet<StepTagSubscription> StepTagSubscriptions => Set<StepTagSubscription>();
    public DbSet<User> Users => Set<User>();
    public DbSet<PipelineClient> PipelineClients => Set<PipelineClient>();
    public DbSet<DataFactory> DataFactories => Set<DataFactory>();
    public DbSet<SynapseWorkspace> SynapseWorkspaces => Set<SynapseWorkspace>();
    public DbSet<AzureCredential> AzureCredentials => Set<AzureCredential>();
    public DbSet<ServicePrincipalAzureCredential> ServicePrincipalCredentials => Set<ServicePrincipalAzureCredential>();
    public DbSet<OrganizationalAccountAzureCredential> OrganizationalAccountCredentials => Set<OrganizationalAccountAzureCredential>();
    public DbSet<ManagedIdentityAzureCredential> ManagedIdentityCredentials => Set<ManagedIdentityAzureCredential>();
    public DbSet<AccessToken> AccessTokens => Set<AccessToken>();
    public DbSet<FunctionApp> FunctionApps => Set<FunctionApp>();
    public DbSet<SqlConnectionBase> SqlConnections => Set<SqlConnectionBase>();
    public DbSet<MsSqlConnection> MsSqlConnections => Set<MsSqlConnection>();
    public DbSet<AnalysisServicesConnection> AnalysisServicesConnections => Set<AnalysisServicesConnection>();
    public DbSet<SnowflakeConnection> SnowflakeConnections => Set<SnowflakeConnection>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<StepTag> StepTags => Set<StepTag>();
    public DbSet<JobTag> JobTags => Set<JobTag>();
    public DbSet<ScheduleTag> ScheduleTags => Set<ScheduleTag>();
    public DbSet<ScdTable> ScdTables => Set<ScdTable>();
    public DbSet<MasterDataTable> MasterDataTables => Set<MasterDataTable>();
    public DbSet<MasterDataTableCategory> MasterDataTableCategories => Set<MasterDataTableCategory>();
    public DbSet<QlikCloudEnvironment> QlikCloudEnvironments => Set<QlikCloudEnvironment>();
    public DbSet<DatabricksWorkspace> DatabricksWorkspaces => Set<DatabricksWorkspace>();
    public DbSet<DbtAccount> DbtAccounts => Set<DbtAccount>();
    public DbSet<StepDataObject> StepDataObjects => Set<StepDataObject>();
    public DbSet<BlobStorageClient> BlobStorageClients => Set<BlobStorageClient>();
    public DbSet<EnvironmentVersion> EnvironmentVersions => Set<EnvironmentVersion>();
    public DbSet<Credential> Credentials => Set<Credential>();
    public DbSet<Proxy> Proxies => Set<Proxy>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<FabricWorkspace> FabricWorkspaces => Set<FabricWorkspace>();
    #endregion

    protected virtual void ConfigureSqlServer(SqlServerDbContextOptionsBuilder options)
    {
        options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        options.MigrationsHistoryTable("__MigrationsHistory", "app");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(_connectionString, ConfigureSqlServer);
        optionsBuilder.EnableDetailedErrors();
        optionsBuilder.EnableSensitiveDataLogging();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("app");
        modelBuilder.ApplyConfigurations(this);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Add(_ => new EnumConvention());

        // Default all foreign keys to no action.
        // There are many potential cascading paths in the model. It is easier to define cascading paths explicitly.
        configurationBuilder.Conventions.Remove<CascadeDeleteConvention>();
        configurationBuilder.Conventions.Remove<SqlServerOnDeleteConvention>();

        // The model contains relatively many navigation properties compared to the data amounts being processed.
        // Therefore, it is better to skip creating indexes for all foreign keys / navigation properties.
        // Actually useful and needed indexes can be created explicitly.
        configurationBuilder.Conventions.Remove<ForeignKeyIndexConvention>();
    }

    protected virtual void OnSavingChanges(object? sender, SavingChangesEventArgs e)
    {
        var now = DateTimeOffset.Now;

        foreach (var entry in ChangeTracker.Entries())
        {
            switch (entry)
            {
                case { State: EntityState.Added, Entity: IAuditable added }:
                    added.CreatedOn = now;
                    added.CreatedBy = Username;
                    added.LastModifiedOn = now;
                    added.LastModifiedBy = Username;
                    continue;
                case { State: EntityState.Modified, Entity: IAuditable modified }:
                    modified.LastModifiedOn = now;
                    modified.LastModifiedBy = Username;
                    break;
            }
        }
    }
}
