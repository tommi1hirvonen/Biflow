using Biflow.DataAccess.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Biflow.DataAccess;

public class BiflowContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public BiflowContext(DbContextOptions<BiflowContext> options, IHttpContextAccessor? httpContextAccessor = null)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

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
    public DbSet<SourceTargetObject> SourceTargetObjects => Set<SourceTargetObject>();
    public DbSet<Execution> Executions => Set<Execution>();
    public DbSet<StepExecution> StepExecutions => Set<StepExecution>();
    public DbSet<StepExecutionAttempt> StepExecutionAttempts => Set<StepExecutionAttempt>();
    public DbSet<Dependency> Dependencies => Set<Dependency>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<User> Users => Set<User>();
    public DbSet<DataFactory> DataFactories => Set<DataFactory>();
    public DbSet<AppRegistration> AppRegistrations => Set<AppRegistration>();
    public DbSet<AccessToken> AccessTokens => Set<AccessToken>();
    public DbSet<FunctionApp> FunctionApps => Set<FunctionApp>();
    public DbSet<ConnectionInfoBase> Connections => Set<ConnectionInfoBase>();
    public DbSet<SqlConnectionInfo> SqlConnections => Set<SqlConnectionInfo>();
    public DbSet<AnalysisServicesConnectionInfo> AnalysisServicesConnections => Set<AnalysisServicesConnectionInfo>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<PackageStepParameter> PackageParameters => Set<PackageStepParameter>();
    public DbSet<StepExecutionParameter> ExecutionParameters => Set<StepExecutionParameter>();
    public DbSet<DataTable> DataTables => Set<DataTable>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("biflow");

        var executionStatusConverter = new EnumToStringConverter<ExecutionStatus>();
        var subscriptionTypeConverter = new EnumToStringConverter<SubscriptionType>();
        var stepTypeConverter = new EnumToStringConverter<StepType>();

        modelBuilder.Entity<Execution>(e =>
        {
            e.ToTable("Execution")
            .Property(e => e.ExecutionStatus)
            .HasConversion(executionStatusConverter);
            e.Property(p => p.NotifyCaller).HasConversion(subscriptionTypeConverter);

            if (_httpContextAccessor is not null)
            {
                e.HasQueryFilter(exec =>
                _httpContextAccessor.HttpContext == null ||
                // The user is either admin or editor or is granted authorization to the job.
                _httpContextAccessor.HttpContext.User.Identity != null &&
                    (_httpContextAccessor.HttpContext.User.IsInRole("Admin") ||
                    _httpContextAccessor.HttpContext.User.IsInRole("Editor") ||
                    Users.Any(u => u.Username == _httpContextAccessor.HttpContext.User.Identity.Name && (u.AuthorizeAllJobs || u.Jobs.Any(j => j.JobId == exec.JobId))))
                );
            }
        });

        modelBuilder.Entity<ExecutionConcurrency>(e =>
        {
            e.ToTable("ExecutionConcurrency")
            .HasOne(c => c.Execution)
            .WithMany(ex => ex.ExecutionConcurrencies);
            e.HasKey("ExecutionId", "StepType");
            e.Property(p => p.StepType).HasConversion(stepTypeConverter);
        });

        var parameterValueTypeConverter = new EnumToStringConverter<ParameterValueType>();

        modelBuilder.Entity<ExecutionParameter>(e =>
        {
            e.ToTable("ExecutionParameter")
            .HasKey(p => new { p.ExecutionId, p.ParameterId });
            e.HasOne(p => p.Execution)
            .WithMany(e => e.ExecutionParameters);
            e.Property(p => p.ParameterValueType).HasConversion(parameterValueTypeConverter);
        });

        modelBuilder.Entity<StepExecution>(e =>
        {
            e.ToTable("ExecutionStep")
            .HasKey(step => new { step.ExecutionId, step.StepId });
            e.Property(e => e.StepType)
            .HasConversion(stepTypeConverter);
            e.HasOne(step => step.Execution)
            .WithMany(e => e.StepExecutions);
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
            .HasValue<EmailStepExecution>(StepType.Email);
        });

        var stepExecutionStatusConverter = new EnumToStringConverter<StepExecutionStatus>();

        modelBuilder.Entity<StepExecutionAttempt>(e =>
        {
            e.ToTable("ExecutionStepAttempt")
            .HasKey(sea => new { sea.ExecutionId, sea.StepId, sea.RetryAttemptIndex });
            e.Property(sea => sea.StepType)
            .HasConversion(stepTypeConverter);
            e.Property(sea => sea.ExecutionStatus)
            .HasConversion(stepExecutionStatusConverter);
            e.HasOne(sea => sea.StepExecution)
            .WithMany(step => step.StepExecutionAttempts);
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
            .HasValue<EmailStepExecutionAttempt>(StepType.Email);
        });

        var dependencyTypeConverter = new EnumToStringConverter<DependencyType>();

        modelBuilder.Entity<Dependency>(e =>
        {
            e.ToTable("Dependency")
            .HasOne(dependency => dependency.Step)
            .WithMany(step => step.Dependencies);
            e.HasKey(d => new { d.StepId, d.DependantOnStepId });
            e.Property(d => d.DependencyType).HasConversion(dependencyTypeConverter);
        });

        modelBuilder.Entity<ExecutionDependency>(e =>
        {
            e.ToTable("ExecutionDependency")
            .HasOne(d => d.StepExecution)
            .WithMany(e => e.ExecutionDependencies)
            .HasForeignKey(d => new { d.ExecutionId, d.StepId });
            e.HasOne(d => d.DependantOnStepExecution)
            .WithMany(e => e.DependantExecutions)
            .HasForeignKey(d => new { d.ExecutionId, d.DependantOnStepId });
            e.HasKey(d => new { d.ExecutionId, d.StepId, d.DependantOnStepId });
            e.Property(d => d.DependencyType).HasConversion(dependencyTypeConverter);
        });

        modelBuilder.Entity<Job>(e =>
        {
            e.ToTable("Job")
            .HasMany(job => job.Steps)
            .WithOne(step => step.Job!);
            e.HasMany(job => job.Schedules)
            .WithOne(schedule => schedule.Job);
            e.HasMany(job => job.Subscriptions)
            .WithOne(subscription => subscription.Job);
            e.HasMany(j => j.Executions)
            .WithOne(e => e.Job!)
            .IsRequired(false);
            e.HasMany(job => job.JobParameters)
            .WithOne(param => param.Job);

            e.HasMany(t => t.Users)
            .WithMany(s => s.Jobs)
            .UsingEntity<Dictionary<string, object>>("JobAuthorization",
            x => x.HasOne<User>().WithMany().HasForeignKey("Username"),
            x => x.HasOne<Job>().WithMany().HasForeignKey("JobId"));

            if (_httpContextAccessor is not null)
            {
                e.HasQueryFilter(j =>
                _httpContextAccessor.HttpContext == null ||
                // The user is either admin or editor or is granted authorization to the job.
                _httpContextAccessor.HttpContext.User.Identity != null &&
                    (_httpContextAccessor.HttpContext.User.IsInRole("Admin") ||
                    _httpContextAccessor.HttpContext.User.IsInRole("Editor") ||
                    Users.Any(u => u.Username == _httpContextAccessor.HttpContext.User.Identity.Name && u.AuthorizeAllJobs) ||
                    j.Users.Any(u => u.Username == _httpContextAccessor.HttpContext.User.Identity.Name))
                );
            }
        });

        modelBuilder.Entity<JobConcurrency>(e =>
        {
            e.ToTable("JobConcurrency")
            .HasOne(c => c.Job)
            .WithMany(j => j.JobConcurrencies);
            e.HasKey("JobId", "StepType");
            e.Property(p => p.StepType).HasConversion(stepTypeConverter);
        });

        modelBuilder.Entity<JobParameter>()
            .ToTable("JobParameter")
            .Property(p => p.ParameterValueType)
            .HasConversion(parameterValueTypeConverter);

        modelBuilder.Entity<Step>(e =>
        {
            e.ToTable("Step")
            .HasOne(step => step.Job!)
            .WithMany(job => job.Steps)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
            e.Property(s => s.StepType)
            .HasConversion(stepTypeConverter);
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
            .HasValue<EmailStep>(StepType.Email);
            e.HasMany(s => s.StepExecutions)
            .WithOne(e => e.Step!)
            .IsRequired(false);
        });

        modelBuilder.Entity<JobStep>()
            .HasOne(step => step.JobToExecute)
            .WithMany(job => job.JobSteps)
            .HasForeignKey(step => step.JobToExecuteId);

        var tagColorConverter = new EnumToStringConverter<TagColor>();
        modelBuilder.Entity<Tag>(e =>
        {
            e.ToTable("Tag")
            .HasMany(t => t.Steps)
            .WithMany(s => s.Tags)
            .UsingEntity<Dictionary<string, object>>("StepTag",
            x => x.HasOne<Step>().WithMany().HasForeignKey("StepId"),
            x => x.HasOne<Tag>().WithMany().HasForeignKey("TagId"));
            
            e.HasMany(t => t.Schedules)
            .WithMany(s => s.Tags)
            .UsingEntity<Dictionary<string, object>>("ScheduleTag",
            x => x.HasOne<Schedule>().WithMany().HasForeignKey("ScheduleId"),
            x => x.HasOne<Tag>().WithMany().HasForeignKey("TagId"));

            e.Property(p => p.Color).HasConversion(tagColorConverter);
        });

        modelBuilder.Entity<SourceTargetObject>(e =>
        {
            e.ToTable("SourceTargetObject");
            e.HasMany(o => o.Sources)
            .WithMany(s => s.Sources)
            .UsingEntity<Dictionary<string, object>>("StepSource",
            x => x.HasOne<Step>().WithMany().HasForeignKey("StepId"),
            x => x.HasOne<SourceTargetObject>().WithMany().HasForeignKey("ObjectId"));

            e.HasMany(o => o.Targets)
            .WithMany(t => t.Targets)
            .UsingEntity<Dictionary<string, object>>("StepTarget",
            x => x.HasOne<Step>().WithMany().HasForeignKey("StepId"),
            x => x.HasOne<SourceTargetObject>().WithMany().HasForeignKey("ObjectId"));
        });

        modelBuilder.Entity<ExecutionSourceTargetObject>(e =>
        {
            e.ToTable("ExecutionSourceTargetObject")
            .HasKey(o => new { o.ExecutionId, o.ObjectId });
            e.HasMany(o => o.Sources)
            .WithMany(s => s.Sources)
            .UsingEntity<Dictionary<string, object>>("ExecutionStepSource",
            x => x.HasOne<StepExecution>().WithMany().HasForeignKey("ExecutionId", "StepId"),
            x => x.HasOne<ExecutionSourceTargetObject>().WithMany().HasForeignKey("ExecutionId", "ObjectId"));

            e.HasMany(o => o.Targets)
            .WithMany(t => t.Targets)
            .UsingEntity<Dictionary<string, object>>("ExecutionStepTarget",
            x => x.HasOne<StepExecution>().WithMany().HasForeignKey("ExecutionId", "StepId"),
            x => x.HasOne<ExecutionSourceTargetObject>().WithMany().HasForeignKey("ExecutionId", "ObjectId"));
        });


        modelBuilder.Entity<Schedule>()
            .ToTable("Schedule")
            .HasOne(schedule => schedule.Job)
            .WithMany(job => job.Schedules)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        var parameterLevelConverter = new EnumToStringConverter<ParameterLevel>();

        modelBuilder.Entity<PackageStepParameter>(e =>
        {
            e.Property(p => p.ParameterLevel).HasConversion(parameterLevelConverter);
        });

        var parameterTypeConverter = new EnumToStringConverter<ParameterType>();

        modelBuilder.Entity<StepParameterBase>(e =>
        {
            e.ToTable("StepParameter")
            .HasOne(parameter => parameter.Step)
            .WithMany(step => step.StepParameters)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
            e.HasDiscriminator<ParameterType>("ParameterType")
            .HasValue<StepParameter>(ParameterType.Base)
            .HasValue<PackageStepParameter>(ParameterType.Package);
            e.Property(p => p.ParameterValueType).HasConversion(parameterValueTypeConverter);
            e.Property(p => p.ParameterType).HasConversion(parameterTypeConverter);
        });


        modelBuilder.Entity<PackageStepExecutionParameter>(e =>
        {
            e.Property(p => p.ParameterLevel).HasConversion(parameterLevelConverter);
        });
        modelBuilder.Entity<StepExecutionParameterBase>(e =>
        {
            e.ToTable("ExecutionStepParameter")
            .HasKey(param => new { param.ExecutionId, param.StepId, param.ParameterId });
            e.HasOne(param => param.StepExecution)
            .WithMany(e => e.StepExecutionParameters)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.ExecutionParameter)
            .WithMany(p => p!.StepExecutionParameters)
            .HasForeignKey(p => new { p.ExecutionId, p.ExecutionParameterId })
            .IsRequired(false);
            e.HasDiscriminator<ParameterType>("ParameterType")
            .HasValue<StepExecutionParameter>(ParameterType.Base)
            .HasValue<PackageStepExecutionParameter>(ParameterType.Package);
            e.Property(p => p.ParameterValueType).HasConversion(parameterValueTypeConverter);
            e.Property(p => p.ParameterType).HasConversion(parameterTypeConverter);
        });


        modelBuilder.Entity<Subscription>(e =>
        {
            e.ToTable("Subscription")
            .HasOne(subscription => subscription.Job)
            .WithMany(job => job.Subscriptions)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(subscription => subscription.User)
            .WithMany(user => user.Subscriptions)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
            e.Property(s => s.SubscriptionType)
            .HasConversion(subscriptionTypeConverter);
            e.HasKey(s => new { s.JobId, s.Username });
        });

        modelBuilder.Entity<User>()
            .ToTable("User")
            .HasMany(user => user.Subscriptions)
            .WithOne(subscription => subscription.User);

        modelBuilder.Entity<AppRegistration>()
            .ToTable("AppRegistration");

        modelBuilder.Entity<AccessToken>()
            .ToTable("AccessToken");
        modelBuilder.Entity<AccessToken>()
            .HasKey(at => new { at.AppRegistrationId, at.ResourceUrl });

        modelBuilder.Entity<DataFactory>()
            .ToTable("DataFactory");

        var connectionTypeConverter = new EnumToStringConverter<ConnectionType>();

        modelBuilder.Entity<ConnectionInfoBase>(e =>
        {
            e.ToTable("Connection");
            e.Property(p => p.ConnectionType)
            .HasConversion(connectionTypeConverter);
            e.HasDiscriminator<ConnectionType>("ConnectionType")
            .HasValue<SqlConnectionInfo>(ConnectionType.Sql)
            .HasValue<AnalysisServicesConnectionInfo>(ConnectionType.AnalysisServices);
        });

        modelBuilder.Entity<FunctionApp>()
            .ToTable("FunctionApp");

        modelBuilder.Entity<DataTable>(e =>
        {
            e.ToTable("DataTable");
            e.HasMany(t => t.Users)
            .WithMany(u => u.DataTables)
            .UsingEntity<Dictionary<string, object>>("DataTableAuthorization",
            x => x.HasOne<User>().WithMany().HasForeignKey("Username"),
            x => x.HasOne<DataTable>().WithMany().HasForeignKey("DataTableId"));

            if (_httpContextAccessor is not null)
            {
                e.HasQueryFilter(t =>
                _httpContextAccessor.HttpContext == null ||
                // The user is either admin or editor or is granted authorization to the data table.
                _httpContextAccessor.HttpContext.User.Identity != null &&
                    (_httpContextAccessor.HttpContext.User.IsInRole("Admin") ||
                    _httpContextAccessor.HttpContext.User.IsInRole("Editor") ||
                    Users.Any(u => u.Username == _httpContextAccessor.HttpContext.User.Identity.Name && u.AuthorizeAllDataTables) ||
                    t.Users.Any(u => u.Username == _httpContextAccessor.HttpContext.User.Identity.Name))
                );
            }
        });
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
