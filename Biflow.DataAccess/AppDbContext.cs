using Biflow.DataAccess.Configuration;
using Biflow.DataAccess.Convention;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace Biflow.DataAccess;

public class AppDbContext : DbContext
{
    private readonly string _connectionString;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public AppDbContext(IConfiguration configuration, IHttpContextAccessor? httpContextAccessor = null)
    {
        _connectionString = configuration.GetConnectionString("AppDbContext")
            ?? throw new ApplicationException("Connection string not found");
        _httpContextAccessor = httpContextAccessor;

        SavingChanges += OnSavingChanges;
    }

    internal string? Username => _httpContextAccessor?.HttpContext?.User.Identity?.Name;

    internal IEnumerable<string>? UserRoles => _httpContextAccessor?.HttpContext?.User
        .FindAll(ClaimTypes.Role)
        .Select(r => r.Value);

    #region DbSets
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<Step> Steps => Set<Step>();
    public DbSet<DatasetStep> DatasetSteps => Set<DatasetStep>();
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
    public DbSet<DataObject> DataObjects => Set<DataObject>();
    public DbSet<Execution> Executions => Set<Execution>();
    public DbSet<StepExecution> StepExecutions => Set<StepExecution>();
    public DbSet<StepExecutionAttempt> StepExecutionAttempts => Set<StepExecutionAttempt>();
    public DbSet<Dependency> Dependencies => Set<Dependency>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<JobSubscription> JobSubscriptions => Set<JobSubscription>();
    public DbSet<JobTagSubscription> JobTagSubscriptions => Set<JobTagSubscription>();
    public DbSet<StepSubscription> StepSubscriptions => Set<StepSubscription>();
    public DbSet<TagSubscription> TagSubscriptions => Set<TagSubscription>();
    public DbSet<User> Users => Set<User>();
    public DbSet<PipelineClient> PipelineClients => Set<PipelineClient>();
    public DbSet<DataFactory> DataFactories => Set<DataFactory>();
    public DbSet<SynapseWorkspace> SynapseWorkspaces => Set<SynapseWorkspace>();
    public DbSet<AppRegistration> AppRegistrations => Set<AppRegistration>();
    public DbSet<AccessToken> AccessTokens => Set<AccessToken>();
    public DbSet<FunctionApp> FunctionApps => Set<FunctionApp>();
    public DbSet<ConnectionInfoBase> Connections => Set<ConnectionInfoBase>();
    public DbSet<SqlConnectionInfo> SqlConnections => Set<SqlConnectionInfo>();
    public DbSet<AnalysisServicesConnectionInfo> AnalysisServicesConnections => Set<AnalysisServicesConnectionInfo>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<MasterDataTable> MasterDataTables => Set<MasterDataTable>();
    public DbSet<MasterDataTableCategory> MasterDataTableCategories => Set<MasterDataTableCategory>();
    public DbSet<JobCategory> JobCategories => Set<JobCategory>();
    public DbSet<QlikCloudClient> QlikCloudClients => Set<QlikCloudClient>();
    public DbSet<StepDataObject> StepDataObjects => Set<StepDataObject>();
    public DbSet<BlobStorageClient> BlobStorageClients => Set<BlobStorageClient>();
    public DbSet<EnvironmentVersion> EnvironmentVersions => Set<EnvironmentVersion>();
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

        // Use reflection to set property access mode for parameter values.
        // This is important for the correct behaviour of parameter types when editing them in forms.
        var parameterBaseType = typeof(ParameterBase);
        var parameterTypes = parameterBaseType.Assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(parameterBaseType) && !t.IsAbstract);
        foreach (var type in parameterTypes)
        {
            modelBuilder.Entity(type)
                .Property(nameof(ParameterBase.ParameterValue))
                .UsePropertyAccessMode(PropertyAccessMode.Property);
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Add(_ => new EnumConvention());

        // Default all foreign keys to no action.
        // There are many potential cascading paths in the model. It is easier to define cascading paths explicitly.
        configurationBuilder.Conventions.Remove<CascadeDeleteConvention>();
        configurationBuilder.Conventions.Remove<SqlServerOnDeleteConvention>();

        // The model contains relatively many navigation properties compared to the data amounts being processed.
        // Therefore it is better to skip creating indexes for all foreign keys / navigation properties.
        // Actually useful and needed indexes can be created explicitly.
        configurationBuilder.Conventions.Remove<ForeignKeyIndexConvention>();
    }

    private void OnSavingChanges(object? sender, SavingChangesEventArgs e)
    {
        var now = DateTimeOffset.Now;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added && entry.Entity is IAuditable added)
            {
                added.CreatedOn = now;
                added.CreatedBy = Username;
                added.LastModifiedOn = now;
                added.LastModifiedBy = Username;
                continue;
            }

            if (entry.State == EntityState.Modified && entry.Entity is IAuditable modified)
            {
                modified.LastModifiedOn = now;
                modified.LastModifiedBy = Username;
                continue;
            }
        }
    }
}
