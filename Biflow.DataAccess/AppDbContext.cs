using Biflow.DataAccess.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Biflow.DataAccess;

public class AppDbContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly string _connectionString;

    private static readonly JsonSerializerOptions IgnoreNullsOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AppDbContext(IConfiguration configuration, IHttpContextAccessor? httpContextAccessor = null)
    {
        _connectionString = configuration.GetConnectionString("AppDbContext")
            ?? throw new ApplicationException("Connection string not found");
        _httpContextAccessor = httpContextAccessor;

        SavingChanges += OnSavingChanges;
    }

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

        #region Common entities

        modelBuilder.Entity<Tag>(e =>
        {
            e.HasMany(t => t.Steps)
            .WithMany(s => s.Tags)
            .UsingEntity<Dictionary<string, object>>("StepTag",
            x => x.HasOne<Step>().WithMany().HasForeignKey("StepId").OnDelete(DeleteBehavior.Cascade),
            x => x.HasOne<Tag>().WithMany().HasForeignKey("TagId").OnDelete(DeleteBehavior.Cascade));

            e.HasMany(t => t.JobSteps)
            .WithMany(s => s.TagFilters)
            .UsingEntity<Dictionary<string, object>>("JobStepTagFilter",
            x => x.HasOne<JobStep>().WithMany().HasForeignKey("StepId").OnDelete(DeleteBehavior.Cascade),
            x => x.HasOne<Tag>().WithMany().HasForeignKey("TagId").OnDelete(DeleteBehavior.Cascade));

            e.HasMany(t => t.Schedules)
            .WithMany(s => s.Tags)
            .UsingEntity<Dictionary<string, object>>("ScheduleTag",
            x => x.HasOne<Schedule>().WithMany().HasForeignKey("ScheduleId").OnDelete(DeleteBehavior.Cascade),
            x => x.HasOne<Tag>().WithMany().HasForeignKey("TagId").OnDelete(DeleteBehavior.Cascade));

            e.HasIndex(p => p.TagName, "UQ_TagName").IsUnique();
        });

        modelBuilder.Entity<AppRegistration>()
            .HasQueryFilter(x => x.DeletedOn == null);

        modelBuilder.Entity<AccessToken>()
            .HasOne(x => x.AppRegistration)
            .WithMany(x => x.AccessTokens)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BlobStorageClient>()
            .HasOne(x => x.AppRegistration)
            .WithMany(x => x.BlobStorageClients)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PipelineClient>(e =>
        {
            e.HasQueryFilter(x => x.DeletedOn == null && x.AppRegistration.DeletedOn == null);
            e.HasDiscriminator<PipelineClientType>("PipelineClientType")
            .HasValue<DataFactory>(PipelineClientType.DataFactory)
            .HasValue<SynapseWorkspace>(PipelineClientType.Synapse);
        });

        modelBuilder.Entity<ConnectionInfoBase>(e =>
        {
            e.HasQueryFilter(x => x.DeletedOn == null);
            e.HasDiscriminator<ConnectionType>("ConnectionType")
            .HasValue<SqlConnectionInfo>(ConnectionType.Sql)
            .HasValue<AnalysisServicesConnectionInfo>(ConnectionType.AnalysisServices);
        });

        modelBuilder.Entity<FunctionApp>()
            .HasQueryFilter(x => x.DeletedOn == null && x.AppRegistration.DeletedOn == null);

        modelBuilder.Entity<QlikCloudClient>(e =>
        {
            e.HasQueryFilter(x => x.DeletedOn == null);
            e.HasMany(c => c.Steps)
            .WithOne(s => s.QlikCloudClient);
        });

        modelBuilder.Entity<DataObject>()
            .HasIndex(p => p.ObjectUri, "UQ_DataObject")
            .IsUnique();

        modelBuilder.Entity<User>(e =>
        {
            e.HasMany(user => user.Subscriptions)
            .WithOne(subscription => subscription.User);

            e.HasIndex(p => p.Username, "UQ_User").IsUnique();

            e.HasMany(u => u.Jobs)
            .WithMany(j => j.Users)
            .UsingEntity<Dictionary<string, object>>("JobAuthorization",
            x => x.HasOne<Job>().WithMany().HasForeignKey("JobId").OnDelete(DeleteBehavior.Cascade),
            x => x.HasOne<User>().WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade));

            e.HasMany(u => u.DataTables)
            .WithMany(t => t.Users)
            .UsingEntity<Dictionary<string, object>>("DataTableAuthorization",
            x => x.HasOne<MasterDataTable>().WithMany().HasForeignKey("DataTableId").OnDelete(DeleteBehavior.Cascade),
            x => x.HasOne<User>().WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade));

            // Create shadow property to be used by Dapper/ADO.NET access in Ui.Core authentication.
            e.Property<string?>("PasswordHash")
                .HasColumnName("PasswordHash")
                .HasColumnType("varchar(100)");
        });

        #endregion

        #region Job

        modelBuilder.Entity<JobCategory>()
            .HasIndex(p => p.CategoryName, "UQ_JobCategory")
            .IsUnique();

        modelBuilder.Entity<Job>(e =>
        {
            e.ToTable(t => t.HasTrigger("Trigger_Job"));

            e.HasOne(j => j.Category)
            .WithMany(c => c.Jobs)
            .OnDelete(DeleteBehavior.SetNull);

            if (_httpContextAccessor is not null)
            {
                e.HasQueryFilter(j =>
                    _httpContextAccessor.HttpContext == null ||
                    // The user is either admin or editor or is granted authorization to the job.
                    _httpContextAccessor.HttpContext.User.Identity != null && (
                        _httpContextAccessor.HttpContext.User.IsInRole(Roles.Admin) ||
                        _httpContextAccessor.HttpContext.User.IsInRole(Roles.Editor) ||
                        Users.Any(u => u.Username == _httpContextAccessor.HttpContext.User.Identity.Name && u.AuthorizeAllJobs) ||
                        j.Users.Any(u => u.Username == _httpContextAccessor.HttpContext.User.Identity.Name)
                    )
                );
            }
        });

        modelBuilder.Entity<JobConcurrency>()
            .HasOne(x => x.Job)
            .WithMany(x => x.JobConcurrencies)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<JobParameter>(e =>
        {
            e.ToTable(t => t.HasTrigger("Trigger_JobParameter"));
            e.OwnsOne(s => s.Expression, ece =>
            {
                ece.Property(p => p.Expression).HasColumnName("Expression");
            });
            e.HasIndex(x => new { x.JobId, x.ParameterName }, "UQ_JobParameter")
            .IsUnique();
            e.HasOne(x => x.Job)
            .WithMany(x => x.JobParameters)
            .OnDelete(DeleteBehavior.ClientCascade);
        });

        modelBuilder.Entity<Schedule>(e =>
        {
            e.HasOne(schedule => schedule.Job)
            .WithMany(job => job.Schedules)
            .OnDelete(DeleteBehavior.Cascade);
            
            e.HasIndex(x => new { x.JobId, x.CronExpression }, "UQ_Schedule")
            .IsUnique();
        });

        #endregion

        #region Step

        modelBuilder.Entity<Step>(e =>
        {
            e.ToTable(t => t.HasTrigger("Trigger_Step"));

            e.HasDiscriminator<StepType>("StepType")
            .HasValue<DatasetStep>(StepType.Dataset)
            .HasValue<ExeStep>(StepType.Exe)
            .HasValue<JobStep>(StepType.Job)
            .HasValue<PackageStep>(StepType.Package)
            .HasValue<PipelineStep>(StepType.Pipeline)
            .HasValue<SqlStep>(StepType.Sql)
            .HasValue<FunctionStep>(StepType.Function)
            .HasValue<AgentJobStep>(StepType.AgentJob)
            .HasValue<TabularStep>(StepType.Tabular)
            .HasValue<EmailStep>(StepType.Email)
            .HasValue<QlikStep>(StepType.Qlik);

            e.OwnsOne(s => s.ExecutionConditionExpression, ece =>
            {
                ece.Property(p => p.Expression).HasColumnName("ExecutionConditionExpression");
            });

            e.HasOne(x => x.Job)
            .WithMany(x => x.Steps)
            .OnDelete(DeleteBehavior.ClientCascade);
        });

        modelBuilder.Entity<SqlStep>()
            .HasOne(x => x.ResultCaptureJobParameter)
            .WithMany(x => x.CapturingSteps)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<JobStep>()
            .HasOne(step => step.JobToExecute)
            .WithMany(job => job.JobSteps)
            .HasForeignKey(step => step.JobToExecuteId)
            .OnDelete(DeleteBehavior.ClientCascade);

        modelBuilder.Entity<Dependency>(e =>
        {
            e.HasOne(dependency => dependency.Step)
            .WithMany(step => step.Dependencies)
            .OnDelete(DeleteBehavior.ClientCascade);

            e.HasOne(dependency => dependency.DependantOnStep)
            .WithMany(step => step.Depending)
            .OnDelete(DeleteBehavior.ClientCascade);
            
            e.ToTable(t => t.HasCheckConstraint("CK_Dependency",
                $"[{nameof(Dependency.StepId)}]<>[{nameof(Dependency.DependantOnStepId)}]"));
        });

        modelBuilder.Entity<StepDataObject>(e =>
        {
            e.HasOne(x => x.Step).WithMany(x => x.DataObjects).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.DataObject).WithMany(x => x.Steps).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExecutionConditionParameter>(e =>
        {
            e.HasOne(x => x.Step)
            .WithMany(x => x.ExecutionConditionParameters)
            .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.JobParameter)
            .WithMany(x => x.ExecutionConditionParameters)
            .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => new { x.StepId, x.ParameterName }, "UQ_StepConditionParameter")
            .IsUnique();
        });

        modelBuilder.Entity<StepParameterBase>(e =>
        {
            e.HasOne(p => p.InheritFromJobParameter)
            .WithMany(p => p.InheritingStepParameters)
            .OnDelete(DeleteBehavior.ClientSetNull);

            e.HasDiscriminator<ParameterType>("ParameterType")
            .HasValue<SqlStepParameter>(ParameterType.Sql)
            .HasValue<PackageStepParameter>(ParameterType.Package)
            .HasValue<JobStepParameter>(ParameterType.Job)
            .HasValue<ExeStepParameter>(ParameterType.Exe)
            .HasValue<FunctionStepParameter>(ParameterType.Function)
            .HasValue<PipelineStepParameter>(ParameterType.Pipeline)
            .HasValue<EmailStepParameter>(ParameterType.Email);

            e.OwnsOne(s => s.Expression, ece =>
            {
                ece.Property(p => p.Expression).HasColumnName("Expression");
            });
        });

        modelBuilder.Entity<SqlStepParameter>()
            .HasOne(p => p.Step)
            .WithMany(p => p.StepParameters)
            .OnDelete(DeleteBehavior.ClientCascade);
        modelBuilder.Entity<PackageStepParameter>()
            .HasOne(p => p.Step)
            .WithMany(p => p.StepParameters)
            .OnDelete(DeleteBehavior.ClientCascade);
        modelBuilder.Entity<PackageStepParameter>()
            .HasIndex(x => new { x.StepId, x.ParameterLevel, x.ParameterName }, "UQ_StepParameter")
            .HasFilter(null)
            .IsUnique();
        modelBuilder.Entity<ExeStepParameter>()
            .HasOne(p => p.Step)
            .WithMany(p => p.StepParameters)
            .OnDelete(DeleteBehavior.ClientCascade);
        modelBuilder.Entity<FunctionStepParameter>()
            .HasOne(p => p.Step)
            .WithMany(p => p.StepParameters)
            .OnDelete(DeleteBehavior.ClientCascade);
        modelBuilder.Entity<PipelineStepParameter>()
            .HasOne(p => p.Step)
            .WithMany(p => p.StepParameters)
            .OnDelete(DeleteBehavior.ClientCascade);
        modelBuilder.Entity<EmailStepParameter>()
            .HasOne(p => p.Step)
            .WithMany(p => p.StepParameters)
            .OnDelete(DeleteBehavior.ClientCascade);
        modelBuilder.Entity<JobStepParameter>(e =>
        {
            e.HasOne(p => p.Step)
            .WithMany(p => p.StepParameters)
            .IsRequired()
            .OnDelete(DeleteBehavior.ClientCascade);
            e.HasOne(p => p.AssignToJobParameter)
            .WithMany(p => p.AssigningStepParameters)
            .OnDelete(DeleteBehavior.ClientCascade);
        });

        modelBuilder.Entity<StepParameterExpressionParameter>(e =>
        {
            e.HasOne(x => x.InheritFromJobParameter)
            .WithMany(x => x.InheritingStepParameterExpressionParameters)
            .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.StepParameter)
            .WithMany(x => x.ExpressionParameters)
            .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.StepParameterId, x.ParameterName }, "UQ_StepParameterExpressionParameter")
            .IsUnique();
        });

        #endregion

        #region Execution

        modelBuilder.Entity<Execution>(e =>
        {
            e.ToTable(t => t.HasTrigger("Trigger_Execution"));
            if (_httpContextAccessor is not null)
            {
                e.HasQueryFilter(exec =>
                _httpContextAccessor.HttpContext == null ||
                // The user is either admin or editor or is granted authorization to the job.
                _httpContextAccessor.HttpContext.User.Identity != null &&
                    (_httpContextAccessor.HttpContext.User.IsInRole(Roles.Admin) ||
                    _httpContextAccessor.HttpContext.User.IsInRole(Roles.Editor) ||
                    Users.Any(u => u.Username == _httpContextAccessor.HttpContext.User.Identity.Name && (u.AuthorizeAllJobs || u.Jobs.Any(j => j.JobId == exec.JobId))))
                );
            }
            e.Property(p => p.ParentExecution).HasConversion(
                from => JsonSerializer.Serialize(from, null as JsonSerializerOptions),
                to => JsonSerializer.Deserialize<StepExecutionAttemptReference?>(to, null as JsonSerializerOptions));

            e.HasIndex(x => new { x.CreatedOn, x.EndedOn }, "IX_Execution_CreatedOn_EndedOn");
            e.HasIndex(x => x.ExecutionStatus, "IX_Execution_ExecutionStatus");
            e.HasIndex(x => new { x.JobId, x.CreatedOn }, "IX_Execution_JobId_CreatedOn");
        });

        modelBuilder.Entity<ExecutionDataObject>()
            .HasOne(x => x.Execution)
            .WithMany(x => x.DataObjects)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExecutionConcurrency>()
            .HasOne(x => x.Execution)
            .WithMany(x => x.ExecutionConcurrencies)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExecutionParameter>(e =>
        {
            e.ToTable(t => t.HasTrigger("Trigger_ExecutionParameter"));
            e.OwnsOne(s => s.Expression, ece =>
            {
                ece.Property(p => p.Expression).HasColumnName("Expression");
            });
            e.HasOne(x => x.Execution)
            .WithMany(x => x.ExecutionParameters)
            .OnDelete(DeleteBehavior.ClientCascade);
        });

        #endregion

        #region Step execution

        modelBuilder.Entity<StepExecution>(e =>
        {
            e.ToTable(t => t.HasTrigger("Trigger_ExecutionStep"));

            e.HasDiscriminator<StepType>("StepType")
            .HasValue<DatasetStepExecution>(StepType.Dataset)
            .HasValue<ExeStepExecution>(StepType.Exe)
            .HasValue<JobStepExecution>(StepType.Job)
            .HasValue<PackageStepExecution>(StepType.Package)
            .HasValue<PipelineStepExecution>(StepType.Pipeline)
            .HasValue<SqlStepExecution>(StepType.Sql)
            .HasValue<FunctionStepExecution>(StepType.Function)
            .HasValue<AgentJobStepExecution>(StepType.AgentJob)
            .HasValue<TabularStepExecution>(StepType.Tabular)
            .HasValue<EmailStepExecution>(StepType.Email)
            .HasValue<QlikStepExecution>(StepType.Qlik);

            e.OwnsOne(s => s.ExecutionConditionExpression, ece =>
            {
                ece.Property(p => p.Expression).HasColumnName("ExecutionConditionExpression");
            });
            e.HasOne(x => x.Execution)
            .WithMany(x => x.StepExecutions)
            .OnDelete(DeleteBehavior.ClientCascade);
        });

        modelBuilder.Entity<SqlStepExecution>()
            .HasOne(x => x.ResultCaptureJobParameter)
            .WithMany(x => x.CapturingStepExecutions)
            .HasForeignKey(x => new { x.ExecutionId, x.ResultCaptureJobParameterId })
            .OnDelete(DeleteBehavior.ClientCascade);

        modelBuilder.Entity<JobStepExecution>()
            .Property(p => p.TagFilters).HasConversion(
                from => JsonSerializer.Serialize(from, null as JsonSerializerOptions),
                to => JsonSerializer.Deserialize<List<JobStepExecution.TagFilter>>(string.IsNullOrEmpty(to) ? "[]" : to, null as JsonSerializerOptions) ?? new(),
                new ValueComparer<List<JobStepExecution.TagFilter>>(
                    (x, y) => x != null && y != null && x.SequenceEqual(y),
                    x => x.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    x => x.ToList()));

        modelBuilder.Entity<StepExecutionAttempt>(e =>
        {
            e.HasDiscriminator<StepType>("StepType")
            .HasValue<DatasetStepExecutionAttempt>(StepType.Dataset)
            .HasValue<ExeStepExecutionAttempt>(StepType.Exe)
            .HasValue<JobStepExecutionAttempt>(StepType.Job)
            .HasValue<PackageStepExecutionAttempt>(StepType.Package)
            .HasValue<PipelineStepExecutionAttempt>(StepType.Pipeline)
            .HasValue<SqlStepExecutionAttempt>(StepType.Sql)
            .HasValue<FunctionStepExecutionAttempt>(StepType.Function)
            .HasValue<AgentJobStepExecutionAttempt>(StepType.AgentJob)
            .HasValue<TabularStepExecutionAttempt>(StepType.Tabular)
            .HasValue<EmailStepExecutionAttempt>(StepType.Email)
            .HasValue<QlikStepExecutionAttempt>(StepType.Qlik);

            e.Property(p => p.InfoMessages).HasConversion(
                from => JsonSerializer.Serialize(from, IgnoreNullsOptions),
                to => JsonSerializer.Deserialize<List<InfoMessage>>(to, IgnoreNullsOptions) ?? new(),
                new ValueComparer<List<InfoMessage>>(
                    (x, y) => x != null && y != null && x.SequenceEqual(y),
                    x => x.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    x => x.ToList()));
            e.Property(p => p.WarningMessages).HasConversion(
                from => JsonSerializer.Serialize(from, IgnoreNullsOptions),
                to => JsonSerializer.Deserialize<List<WarningMessage>>(to, IgnoreNullsOptions) ?? new(),
                new ValueComparer<List<WarningMessage>>(
                    (x, y) => x != null && y != null && x.SequenceEqual(y),
                    x => x.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    x => x.ToList()));
            e.Property(p => p.ErrorMessages).HasConversion(
                from => JsonSerializer.Serialize(from, IgnoreNullsOptions),
                to => JsonSerializer.Deserialize<List<ErrorMessage>>(to, IgnoreNullsOptions) ?? new(),
                new ValueComparer<List<ErrorMessage>>(
                    (x, y) => x != null && y != null && x.SequenceEqual(y),
                    x => x.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    x => x.ToList()));

            e.HasOne(x => x.StepExecution)
            .WithMany(x => x.StepExecutionAttempts)
            .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExecutionDependency>(e =>
        {
            e.HasOne(d => d.StepExecution)
            .WithMany(e => e.ExecutionDependencies)
            .HasForeignKey(d => new { d.ExecutionId, d.StepId })
            .OnDelete(DeleteBehavior.ClientCascade);
            e.HasOne(d => d.DependantOnStepExecution)
            .WithMany(e => e.DependantExecutions)
            .HasForeignKey(d => new { d.ExecutionId, d.DependantOnStepId })
            .OnDelete(DeleteBehavior.ClientCascade);
            e.ToTable(t => t.HasCheckConstraint("CK_ExecutionDependency",
                $"[{nameof(ExecutionDependency.StepId)}]<>[{nameof(ExecutionDependency.DependantOnStepId)}]"));
        });

        modelBuilder.Entity<StepExecutionDataObject>(e =>
        { 
            e.HasOne(x => x.DataObject)
            .WithMany(x => x.StepExecutions)
            .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.StepExecution)
            .WithMany(x => x.DataObjects)
            .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StepExecutionConditionParameter>(e =>
        {
            e.HasOne(p => p.StepExecution)
            .WithMany(e => e.ExecutionConditionParameters)
            .HasForeignKey("ExecutionId", "StepId")
            .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.ExecutionParameter)
            .WithMany(e => e.ExecutionConditionParameters)
            .HasForeignKey("ExecutionId", "ExecutionParameterId")
            .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StepExecutionParameterBase>(e =>
        {
            e.HasOne(p => p.InheritFromExecutionParameter)
            .WithMany(p => p.StepExecutionParameters)
            .HasForeignKey(p => new { p.ExecutionId, p.InheritFromExecutionParameterId })
            .OnDelete(DeleteBehavior.Cascade);

            e.HasDiscriminator<ParameterType>("ParameterType")
            .HasValue<SqlStepExecutionParameter>(ParameterType.Sql)
            .HasValue<PackageStepExecutionParameter>(ParameterType.Package)
            .HasValue<JobStepExecutionParameter>(ParameterType.Job)
            .HasValue<ExeStepExecutionParameter>(ParameterType.Exe)
            .HasValue<FunctionStepExecutionParameter>(ParameterType.Function)
            .HasValue<PipelineStepExecutionParameter>(ParameterType.Pipeline)
            .HasValue<EmailStepExecutionParameter>(ParameterType.Email);

            e.OwnsOne(s => s.Expression, ece =>
            {
                ece.Property(p => p.Expression).HasColumnName("Expression");
            });
        });

        modelBuilder.Entity<SqlStepExecutionParameter>()
            .HasOne(p => p.StepExecution)
            .WithMany(p => p.StepExecutionParameters)
            .HasForeignKey("ExecutionId", "StepId")
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PackageStepExecutionParameter>()
            .HasOne(p => p.StepExecution)
            .WithMany(p => p.StepExecutionParameters)
            .HasForeignKey("ExecutionId", "StepId")
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ExeStepExecutionParameter>()
            .HasOne(p => p.StepExecution)
            .WithMany(p => p.StepExecutionParameters)
            .HasForeignKey("ExecutionId", "StepId")
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<FunctionStepExecutionParameter>()
            .HasOne(p => p.StepExecution)
            .WithMany(p => p.StepExecutionParameters)
            .HasForeignKey("ExecutionId", "StepId")
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PipelineStepExecutionParameter>()
            .HasOne(p => p.StepExecution)
            .WithMany(p => p.StepExecutionParameters)
            .HasForeignKey("ExecutionId", "StepId")
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<EmailStepExecutionParameter>()
            .HasOne(p => p.StepExecution)
            .WithMany(p => p.StepExecutionParameters)
            .HasForeignKey("ExecutionId", "StepId")
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<JobStepExecutionParameter>()
            .HasOne(p => p.StepExecution)
            .WithMany(p => p.StepExecutionParameters)
            .HasForeignKey("ExecutionId", "StepId")
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StepExecutionParameterExpressionParameter>(e =>
        {
            e.HasOne(x => x.StepParameter)
            .WithMany(x => x.ExpressionParameters)
            .HasForeignKey("ExecutionId", "StepParameterId")
            .OnDelete(DeleteBehavior.ClientCascade);

            e.HasOne(x => x.InheritFromExecutionParameter)
            .WithMany(x => x.StepExecutionParameterExpressionParameters)
            .HasForeignKey("ExecutionId", "InheritFromExecutionParameterId")
            .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.ExecutionId, x.StepParameterId, x.ParameterName }, "UQ_ExecutionStepParameterExpressionParameter")
            .IsUnique();
        });

        #endregion

        #region Subscriptions

        modelBuilder.Entity<Subscription>(e =>
        {
            e.HasDiscriminator<SubscriptionType>("SubscriptionType")
            .HasValue<JobSubscription>(SubscriptionType.Job)
            .HasValue<JobTagSubscription>(SubscriptionType.JobTag)
            .HasValue<StepSubscription>(SubscriptionType.Step)
            .HasValue<TagSubscription>(SubscriptionType.Tag);

            e.HasOne(x => x.User)
                .WithMany(x => x.Subscriptions)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<JobSubscription>(e =>
        {
            e.HasOne(x => x.Job)
            .WithMany(x => x.JobSubscriptions)
            .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.UserId, x.JobId }, "IX_UQ_Subscription_JobSubscription")
            .HasFilter($"[{nameof(Subscription.SubscriptionType)}] = '{nameof(SubscriptionType.Job)}'")
            .IsUnique();
        });

        modelBuilder.Entity<JobTagSubscription>(e =>
        {
            e.HasOne(x => x.Job)
            .WithMany(x => x.JobTagSubscriptions)
            .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Tag)
            .WithMany(x => x.JobTagSubscriptions)
            .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.UserId, x.JobId, x.TagId }, "IX_UQ_Subscription_JobTagSubscription")
            .HasFilter($"[{nameof(Subscription.SubscriptionType)}] = '{nameof(SubscriptionType.JobTag)}'")
            .IsUnique();
        });

        modelBuilder.Entity<StepSubscription>(e =>
        {
            e.HasOne(x => x.Step)
            .WithMany(x => x.StepSubscriptions)
            .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.UserId, x.StepId }, "IX_UQ_Subscription_StepSubscription")
            .HasFilter($"[{nameof(Subscription.SubscriptionType)}] = '{nameof(SubscriptionType.Step)}'")
            .IsUnique();
        });

        modelBuilder.Entity<TagSubscription>(e =>
        {
            e.HasOne(x => x.Tag)
            .WithMany(x => x.TagSubscriptions)
            .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.UserId, x.TagId }, "IX_UQ_Subscription_TagSubscription")
            .HasFilter($"[{nameof(Subscription.SubscriptionType)}] = '{nameof(SubscriptionType.Tag)}'")
            .IsUnique();
        });

        #endregion

        #region Data tables

        modelBuilder.Entity<MasterDataTableCategory>()
            .HasIndex(p => p.CategoryName, "UQ_DataTableCategory")
            .IsUnique();

        modelBuilder.Entity<MasterDataTable>(e =>
        {
            e.ToTable(t => t.HasTrigger("Trigger_DataTable"));
            e.HasMany(t => t.Lookups).WithOne(l => l.Table);
            e.HasOne(t => t.Category)
            .WithMany(c => c.Tables)
            .OnDelete(DeleteBehavior.SetNull);

            if (_httpContextAccessor is not null)
            {
                e.HasQueryFilter(t =>
                _httpContextAccessor.HttpContext == null ||
                // The user is either admin or editor or is granted authorization to the data table.
                _httpContextAccessor.HttpContext.User.Identity != null &&
                    (_httpContextAccessor.HttpContext.User.IsInRole(Roles.Admin) ||
                    _httpContextAccessor.HttpContext.User.IsInRole(Roles.DataTableMaintainer) ||
                    Users.Any(u => u.Username == _httpContextAccessor.HttpContext.User.Identity.Name && u.AuthorizeAllDataTables) ||
                    t.Users.Any(u => u.Username == _httpContextAccessor.HttpContext.User.Identity.Name))
                );
            }
        });

        modelBuilder.Entity<MasterDataTableLookup>(e =>
        {
            // Use client cascade because of multiple cascade paths not supported by SQL Server.
            e.HasOne(l => l.Table).WithMany(t => t.Lookups).OnDelete(DeleteBehavior.ClientCascade);
            e.HasOne(l => l.LookupTable).WithMany(t => t.DependentLookups).OnDelete(DeleteBehavior.ClientCascade);
            e.HasIndex(x => new { x.TableId, x.ColumnName }, "UQ_DataTableLookup").IsUnique();
        });

        #endregion

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
        #region Enum conventions

        configurationBuilder.Properties<ExecutionStatus>()
            .HaveConversion<EnumToStringConverter<ExecutionStatus>>()
            .HaveMaxLength(50)
            .AreUnicode(false);

        configurationBuilder.Properties<StepExecutionStatus>()
            .HaveConversion<EnumToStringConverter<StepExecutionStatus>>()
            .HaveMaxLength(50)
            .AreUnicode(false);

        configurationBuilder.Properties<AlertType>()
            .HaveConversion<EnumToStringConverter<AlertType>>()
            .HaveMaxLength(50)
            .AreUnicode(false);

        configurationBuilder.Properties<SubscriptionType>()
            .HaveConversion<EnumToStringConverter<SubscriptionType>>()
            .HaveMaxLength(50)
            .AreUnicode(false);

        configurationBuilder.Properties<StepType>()
            .HaveConversion<EnumToStringConverter<StepType>>()
            .HaveMaxLength(50)
            .AreUnicode(false);

        configurationBuilder.Properties<DuplicateExecutionBehaviour>()
            .HaveConversion<EnumToStringConverter<DuplicateExecutionBehaviour>>()
            .HaveMaxLength(50)
            .AreUnicode(false);

        configurationBuilder.Properties<ParameterValueType>()
            .HaveConversion<EnumToStringConverter<ParameterValueType>>()
            .HaveMaxLength(50)
            .AreUnicode(false);

        configurationBuilder.Properties<DependencyType>()
            .HaveConversion<EnumToStringConverter<DependencyType>>()
            .HaveMaxLength(50)
            .AreUnicode(false);

        configurationBuilder.Properties<TagColor>()
            .HaveConversion<EnumToStringConverter<TagColor>>()
            .HaveMaxLength(50)
            .AreUnicode(false);

        configurationBuilder.Properties<ParameterLevel>()
            .HaveConversion<EnumToStringConverter<ParameterLevel>>()
            .HaveMaxLength(50)
            .AreUnicode(false);

        configurationBuilder.Properties<ParameterType>()
            .HaveConversion<EnumToStringConverter<ParameterType>>()
            .HaveMaxLength(50)
            .AreUnicode(false);

        configurationBuilder.Properties<ConnectionType>()
            .HaveConversion<EnumToStringConverter<ConnectionType>>()
            .HaveMaxLength(50)
            .AreUnicode(false);

        configurationBuilder.Properties<PipelineClientType>()
            .HaveConversion<EnumToStringConverter<PipelineClientType>>()
            .HaveMaxLength(50)
            .AreUnicode(false);

        configurationBuilder.Properties<LookupDisplayType>()
            .HaveConversion<EnumToStringConverter<LookupDisplayType>>()
            .HaveMaxLength(50)
            .AreUnicode(false);

        configurationBuilder.Properties<DataObjectReferenceType>()
            .HaveConversion<EnumToStringConverter<DataObjectReferenceType>>()
            .HaveMaxLength(50)
            .AreUnicode(false);

        configurationBuilder.Properties<BlobStorageConnectionMethod>()
            .HaveConversion<EnumToStringConverter<BlobStorageConnectionMethod>>()
            .HaveMaxLength(50)
            .AreUnicode(false);

        #endregion

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
        var user = _httpContextAccessor?.HttpContext?.User.Identity?.Name;
        var now = DateTimeOffset.Now;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added && entry.Entity is IAuditable added)
            {
                added.CreatedOn = now;
                added.CreatedBy = user;
                added.LastModifiedOn = now;
                added.LastModifiedBy = user;
                continue;
            }

            if (entry.State == EntityState.Modified && entry.Entity is IAuditable modified)
            {
                modified.LastModifiedOn = now;
                modified.LastModifiedBy = user;
                continue;
            }

            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDeletable deleted)
            {
                entry.State = EntityState.Modified;
                deleted.DeletedOn = now;
            }
        }
    }
}
