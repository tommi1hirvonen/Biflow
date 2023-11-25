using Biflow.DataAccess.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Biflow.DataAccess;

public class AppDbContext(IConfiguration configuration, IHttpContextAccessor? httpContextAccessor = null) : DbContext()
{
    private readonly IHttpContextAccessor? _httpContextAccessor = httpContextAccessor;
    private readonly string _connectionString = configuration.GetConnectionString("AppDbContext")
            ?? throw new ApplicationException("Connection string not found");

    private static readonly JsonSerializerOptions IgnoreNullsOptions = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

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
    public DbSet<PackageStepParameter> PackageParameters => Set<PackageStepParameter>();
    public DbSet<MasterDataTable> MasterDataTables => Set<MasterDataTable>();
    public DbSet<MasterDataTableCategory> MasterDataTableCategories => Set<MasterDataTableCategory>();
    public DbSet<JobCategory> JobCategories => Set<JobCategory>();
    public DbSet<QlikCloudClient> QlikCloudClients => Set<QlikCloudClient>();

    protected virtual void ConfigureSqlServer(SqlServerDbContextOptionsBuilder options)
    {
        options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
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
        });

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
        });

        modelBuilder.Entity<JobStepExecution>()
            .Property(p => p.TagFilters).HasConversion(
                from => JsonSerializer.Serialize(from, null as JsonSerializerOptions),
                to => JsonSerializer.Deserialize<List<JobStepExecution.TagFilter>>(string.IsNullOrEmpty(to) ? "[]" : to, null as JsonSerializerOptions) ?? new());

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
                to => JsonSerializer.Deserialize<List<InfoMessage>>(to, IgnoreNullsOptions) ?? new());
            e.Property(p => p.WarningMessages).HasConversion(
                from => JsonSerializer.Serialize(from, IgnoreNullsOptions),
                to => JsonSerializer.Deserialize<List<WarningMessage>>(to, IgnoreNullsOptions) ?? new());
            e.Property(p => p.ErrorMessages).HasConversion(
                from => JsonSerializer.Serialize(from, IgnoreNullsOptions),
                to => JsonSerializer.Deserialize<List<ErrorMessage>>(to, IgnoreNullsOptions) ?? new());
        });

        modelBuilder.Entity<InfoMessage>(e => e.HasNoKey());
        modelBuilder.Entity<WarningMessage>(e => e.HasNoKey());
        modelBuilder.Entity<ErrorMessage>(e => e.HasNoKey());

        modelBuilder.Entity<Dependency>(e =>
        {
            e.HasOne(dependency => dependency.Step)
            .WithMany(step => step.Dependencies);
        });

        modelBuilder.Entity<ExecutionDependency>(e =>
        {
            e.HasOne(d => d.StepExecution)
            .WithMany(e => e.ExecutionDependencies)
            .HasForeignKey(d => new { d.ExecutionId, d.StepId });
            e.HasOne(d => d.DependantOnStepExecution)
            .WithMany(e => e.DependantExecutions)
            .HasForeignKey(d => new { d.ExecutionId, d.DependantOnStepId })
            .IsRequired(false);
        });

        modelBuilder.Entity<Job>(e =>
        {
            e.ToTable(t => t.HasTrigger("Trigger_Job"));
            e.HasMany(t => t.Users)
            .WithMany(s => s.Jobs)
            .UsingEntity<Dictionary<string, object>>("JobAuthorization",
            x => x.HasOne<User>().WithMany().HasForeignKey("UserId"),
            x => x.HasOne<Job>().WithMany().HasForeignKey("JobId"));

            if (_httpContextAccessor is not null)
            {
                e.HasQueryFilter(j =>
                _httpContextAccessor.HttpContext == null ||
                // The user is either admin or editor or is granted authorization to the job.
                _httpContextAccessor.HttpContext.User.Identity != null &&
                    (_httpContextAccessor.HttpContext.User.IsInRole(Roles.Admin) ||
                    _httpContextAccessor.HttpContext.User.IsInRole(Roles.Editor) ||
                    Users.Any(u => u.Username == _httpContextAccessor.HttpContext.User.Identity.Name && u.AuthorizeAllJobs) ||
                    j.Users.Any(u => u.Username == _httpContextAccessor.HttpContext.User.Identity.Name))
                );
            }
        });

        modelBuilder.Entity<Step>(e =>
        {
            e.ToTable(t => t.HasTrigger("Trigger_Step"));
            e.HasOne(step => step.Job)
            .WithMany(job => job.Steps)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
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
            e.HasMany(s => s.StepExecutions)
            .WithOne(e => e.Step)
            .IsRequired(false);
            e.OwnsOne(s => s.ExecutionConditionExpression, ece =>
            {
                ece.Property(p => p.Expression).HasColumnName("ExecutionConditionExpression");
            });
        });

        modelBuilder.Entity<JobStep>()
            .HasOne(step => step.JobToExecute)
            .WithMany(job => job.JobSteps)
            .HasForeignKey(step => step.JobToExecuteId);

        modelBuilder.Entity<Tag>(e =>
        {
            e.HasMany(t => t.Steps)
            .WithMany(s => s.Tags)
            .UsingEntity<Dictionary<string, object>>("StepTag",
            x => x.HasOne<Step>().WithMany().HasForeignKey("StepId"),
            x => x.HasOne<Tag>().WithMany().HasForeignKey("TagId"));

            e.HasMany(t => t.JobSteps)
            .WithMany(s => s.TagFilters)
            .UsingEntity<Dictionary<string, object>>("JobStepTagFilter",
            x => x.HasOne<JobStep>().WithMany().HasForeignKey("StepId"),
            x => x.HasOne<Tag>().WithMany().HasForeignKey("TagId"));

            e.HasMany(t => t.Schedules)
            .WithMany(s => s.Tags)
            .UsingEntity<Dictionary<string, object>>("ScheduleTag",
            x => x.HasOne<Schedule>().WithMany().HasForeignKey("ScheduleId"),
            x => x.HasOne<Tag>().WithMany().HasForeignKey("TagId"));
        });

        modelBuilder.Entity<DataObject>(e =>
        {
            e.HasMany(o => o.Readers)
            .WithMany(s => s.Sources)
            .UsingEntity<Dictionary<string, object>>("StepSource",
            x => x.HasOne<Step>().WithMany().HasForeignKey("StepId"),
            x => x.HasOne<DataObject>().WithMany().HasForeignKey("ObjectId"));

            e.HasMany(o => o.Writers)
            .WithMany(t => t.Targets)
            .UsingEntity<Dictionary<string, object>>("StepTarget",
            x => x.HasOne<Step>().WithMany().HasForeignKey("StepId"),
            x => x.HasOne<DataObject>().WithMany().HasForeignKey("ObjectId"));
        });

        modelBuilder.Entity<ExecutionDataObject>(e =>
        {
            e.HasMany(o => o.Sources)
            .WithMany(s => s.Sources)
            .UsingEntity<Dictionary<string, object>>("ExecutionStepSource",
            x => x.HasOne<StepExecution>().WithMany().HasForeignKey("ExecutionId", "StepId"),
            x => x.HasOne<ExecutionDataObject>().WithMany().HasForeignKey("ExecutionId", "ObjectId"));

            e.HasMany(o => o.Targets)
            .WithMany(t => t.Targets)
            .UsingEntity<Dictionary<string, object>>("ExecutionStepTarget",
            x => x.HasOne<StepExecution>().WithMany().HasForeignKey("ExecutionId", "StepId"),
            x => x.HasOne<ExecutionDataObject>().WithMany().HasForeignKey("ExecutionId", "ObjectId"));
        });

        modelBuilder.Entity<Schedule>()
            .HasOne(schedule => schedule.Job)
            .WithMany(job => job.Schedules)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExecutionParameter>(e =>
        {
            e.ToTable(t => t.HasTrigger("Trigger_ExecutionParameter"));
            e.OwnsOne(s => s.Expression, ece =>
            {
                ece.Property(p => p.Expression).HasColumnName("Expression");
            });
            e.HasMany(p => p.StepExecutionParameterExpressionParameters)
            .WithOne(p => p.InheritFromExecutionParameter)
            .HasForeignKey("ExecutionId", "InheritFromExecutionParameterId");
        });

        modelBuilder.Entity<JobParameter>(e =>
        {
            e.ToTable(t => t.HasTrigger("Trigger_JobParameter"));
            e.OwnsOne(s => s.Expression, ece =>
            {
                ece.Property(p => p.Expression).HasColumnName("Expression");
            });
            e.HasMany(p => p.InheritingStepParameterExpressionParameters).WithOne(p => p.InheritFromJobParameter);
            e.HasMany(p => p.CapturingSteps).WithOne(s => s.ResultCaptureJobParameter);
            e.HasMany(p => p.ExecutionConditionParameters).WithOne(p => p.JobParameter);
        });

        modelBuilder.Entity<StepParameterBase>(e =>
        {
            e.HasOne(p => p.InheritFromJobParameter).WithMany(p => p.InheritingStepParameters);
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
            e.HasMany(p => p.ExpressionParameters).WithOne(p => p.StepParameter);
        });

        modelBuilder.Entity<SqlStepParameter>(e => e.HasOne(p => p.Step).WithMany(p => p.StepParameters));
        modelBuilder.Entity<PackageStepParameter>(e => e.HasOne(p => p.Step).WithMany(p => p.StepParameters));
        modelBuilder.Entity<ExeStepParameter>(e => e.HasOne(p => p.Step).WithMany(p => p.StepParameters));
        modelBuilder.Entity<FunctionStepParameter>(e => e.HasOne(p => p.Step).WithMany(p => p.StepParameters));
        modelBuilder.Entity<PipelineStepParameter>(e => e.HasOne(p => p.Step).WithMany(p => p.StepParameters));
        modelBuilder.Entity<EmailStepParameter>(e => e.HasOne(p => p.Step).WithMany(p => p.StepParameters));
        modelBuilder.Entity<JobStepParameter>(e =>
        {
            e.HasOne(p => p.Step).WithMany(p => p.StepParameters).IsRequired().OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.AssignToJobParameter).WithMany(p => p.AssigningStepParameters);
        });

        modelBuilder.Entity<StepExecutionParameterBase>(e =>
        {
            e.HasOne(p => p.InheritFromExecutionParameter)
            .WithMany(p => p.StepExecutionParameters)
            .HasForeignKey(p => new { p.ExecutionId, p.InheritFromExecutionParameterId })
            .IsRequired(false);
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
            e.HasMany(p => p.ExpressionParameters).WithOne(p => p.StepParameter).HasForeignKey("ExecutionId", "StepParameterId");
        });

        modelBuilder.Entity<SqlStepExecutionParameter>(e => e.HasOne(p => p.StepExecution).WithMany(p => p.StepExecutionParameters).HasForeignKey("ExecutionId", "StepId"));
        modelBuilder.Entity<PackageStepExecutionParameter>(e => e.HasOne(p => p.StepExecution).WithMany(p => p.StepExecutionParameters).HasForeignKey("ExecutionId", "StepId"));
        modelBuilder.Entity<ExeStepExecutionParameter>(e => e.HasOne(p => p.StepExecution).WithMany(p => p.StepExecutionParameters).HasForeignKey("ExecutionId", "StepId"));
        modelBuilder.Entity<FunctionStepExecutionParameter>(e => e.HasOne(p => p.StepExecution).WithMany(p => p.StepExecutionParameters).HasForeignKey("ExecutionId", "StepId"));
        modelBuilder.Entity<PipelineStepExecutionParameter>(e => e.HasOne(p => p.StepExecution).WithMany(p => p.StepExecutionParameters).HasForeignKey("ExecutionId", "StepId"));
        modelBuilder.Entity<EmailStepExecutionParameter>(e => e.HasOne(p => p.StepExecution).WithMany(p => p.StepExecutionParameters).HasForeignKey("ExecutionId", "StepId"));
        modelBuilder.Entity<JobStepExecutionParameter>(e => e.HasOne(p => p.StepExecution).WithMany(p => p.StepExecutionParameters).HasForeignKey("ExecutionId", "StepId"));

        modelBuilder.Entity<StepExecutionConditionParameter>(e =>
        {
            e.HasOne(p => p.StepExecution)
            .WithMany(e => e.ExecutionConditionParameters)
            .HasForeignKey("ExecutionId", "StepId");
            e.HasOne(p => p.ExecutionParameter)
            .WithMany(e => e.ExecutionConditionParameters)
            .HasForeignKey("ExecutionId", "ExecutionParameterId");
        });

        modelBuilder.Entity<Subscription>(e =>
        {
            e.HasDiscriminator<SubscriptionType>("SubscriptionType")
            .HasValue<JobSubscription>(SubscriptionType.Job)
            .HasValue<JobTagSubscription>(SubscriptionType.JobTag)
            .HasValue<StepSubscription>(SubscriptionType.Step)
            .HasValue<TagSubscription>(SubscriptionType.Tag);
        });

        modelBuilder.Entity<User>(e =>
        {
            e.HasMany(user => user.Subscriptions)
            .WithOne(subscription => subscription.User);
        });
            

        modelBuilder.Entity<PipelineClient>(e =>
        {
            e.HasDiscriminator<PipelineClientType>("PipelineClientType")
            .HasValue<DataFactory>(PipelineClientType.DataFactory)
            .HasValue<SynapseWorkspace>(PipelineClientType.Synapse);
        });

        modelBuilder.Entity<ConnectionInfoBase>(e =>
        {
            e.HasDiscriminator<ConnectionType>("ConnectionType")
            .HasValue<SqlConnectionInfo>(ConnectionType.Sql)
            .HasValue<AnalysisServicesConnectionInfo>(ConnectionType.AnalysisServices);
        });

        modelBuilder.Entity<QlikCloudClient>(e =>
        {
            e.HasMany(c => c.Steps)
            .WithOne(s => s.QlikCloudClient);
        });

        modelBuilder.Entity<MasterDataTableLookup>(e =>
        {
            e.HasOne(l => l.Table).WithMany(t => t.Lookups);
            e.HasOne(l => l.LookupTable).WithMany(t => t.DependentLookups).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MasterDataTable>(e =>
        {
            e.HasMany(t => t.Lookups).WithOne(l => l.Table);
            e.HasOne(t => t.Category).WithMany(c => c.Tables).HasForeignKey(p => p.CategoryId);

            e.HasMany(t => t.Users)
            .WithMany(u => u.DataTables)
            .UsingEntity<Dictionary<string, object>>("DataTableAuthorization",
            x => x.HasOne<User>().WithMany().HasForeignKey("UserId"),
            x => x.HasOne<MasterDataTable>().WithMany().HasForeignKey("DataTableId"));

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
        configurationBuilder.Properties<ExecutionStatus>().HaveConversion<EnumToStringConverter<ExecutionStatus>>();
        configurationBuilder.Properties<StepExecutionStatus>().HaveConversion<EnumToStringConverter<StepExecutionStatus>>();
        configurationBuilder.Properties<AlertType>().HaveConversion<EnumToStringConverter<AlertType>>();
        configurationBuilder.Properties<SubscriptionType>().HaveConversion<EnumToStringConverter<SubscriptionType>>();
        configurationBuilder.Properties<StepType>().HaveConversion<EnumToStringConverter<StepType>>();
        configurationBuilder.Properties<DuplicateExecutionBehaviour>().HaveConversion<EnumToStringConverter<DuplicateExecutionBehaviour>>();
        configurationBuilder.Properties<ParameterValueType>().HaveConversion<EnumToStringConverter<ParameterValueType>>();
        configurationBuilder.Properties<DependencyType>().HaveConversion<EnumToStringConverter<DependencyType>>();
        configurationBuilder.Properties<TagColor>().HaveConversion<EnumToStringConverter<TagColor>>();
        configurationBuilder.Properties<ParameterLevel>().HaveConversion<EnumToStringConverter<ParameterLevel>>();
        configurationBuilder.Properties<ParameterType>().HaveConversion<EnumToStringConverter<ParameterType>>();
        configurationBuilder.Properties<ConnectionType>().HaveConversion<EnumToStringConverter<ConnectionType>>();
        configurationBuilder.Properties<PipelineClientType>().HaveConversion<EnumToStringConverter<PipelineClientType>>();
        configurationBuilder.Properties<LookupDisplayType>().HaveConversion<EnumToStringConverter<LookupDisplayType>>();
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var user = _httpContextAccessor?.HttpContext?.User.Identity?.Name;

        // If there are Jobs or Steps that have been edited, set the audit fields.
        var editedJobsAndSteps = ChangeTracker.Entries()
            .Where(entity => (entity.Entity is Job || entity.Entity is Step) && entity.State == EntityState.Modified)
            .ToList();
        editedJobsAndSteps.ForEach(entity =>
        {
            entity.Property("LastModifiedDateTime").CurrentValue = DateTimeOffset.Now;
            entity.Property("LastModifiedBy").CurrentValue = user;
        });

        // If there are Jobs or Steps that have been added, set the audit fields.
        var addedJobsAndSteps = ChangeTracker.Entries().Where(entity => (entity.Entity is Job || entity.Entity is Step) && entity.State == EntityState.Added).ToList();
        addedJobsAndSteps.ForEach(entity =>
        {
            entity.Property("CreatedDateTime").CurrentValue = DateTimeOffset.Now;
            entity.Property("LastModifiedDateTime").CurrentValue = DateTimeOffset.Now;
            entity.Property("CreatedBy").CurrentValue = user;
            entity.Property("LastModifiedBy").CurrentValue = user;
        });

        // Set the audit fields for new dependencies and schedules.
        var addedDependenciesAndSchedules = ChangeTracker
            .Entries()
            .Where(entity => (entity.Entity is Dependency || entity.Entity is Schedule) && entity.State == EntityState.Added)
            .ToList();

        addedDependenciesAndSchedules.ForEach(entity =>
        {
            entity.Property("CreatedDateTime").CurrentValue = DateTimeOffset.Now;
            entity.Property("CreatedBy").CurrentValue = user;
        });

        // Set the audit fields for edited users.
        var editedUsers = ChangeTracker.Entries().Where(entity => entity.Entity is User && entity.State == EntityState.Modified).ToList();
        editedUsers.ForEach(user => user.Property("LastModifiedDateTime").CurrentValue = DateTimeOffset.Now);

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }


}
