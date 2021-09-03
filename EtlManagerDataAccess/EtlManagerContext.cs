using EtlManagerDataAccess.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManagerDataAccess
{
    public class EtlManagerContext : DbContext
    {
        private readonly HttpContext? HttpContext;

        public EtlManagerContext(DbContextOptions<EtlManagerContext> options, IHttpContextAccessor? httpContextAccessor = null)
            : base(options)
        {
            HttpContext = httpContextAccessor?.HttpContext;
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
        public DbSet<Connection> Connections => Set<Connection>();
        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<PackageParameter> PackageParameters => Set<PackageParameter>();
        public DbSet<StepParameter> StepParameters => Set<StepParameter>();
        public DbSet<StepExecutionParameter> ExecutionParameters => Set<StepExecutionParameter>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Func<StepType, string> stepTypeToString = value => value switch
            {
                StepType.Sql => "SQL",
                StepType.Package => "SSIS",
                StepType.Job => "JOB",
                StepType.Exe => "EXE",
                StepType.Pipeline => "PIPELINE",
                StepType.Dataset => "DATASET",
                StepType.Function => "FUNCTION",
                _ => throw new ArgumentNullException(nameof(value), "StepType value cannot be null")
            };
            Func<string, StepType> stringToStepType = value => value switch
            {
                "SQL" => StepType.Sql,
                "SSIS" => StepType.Package,
                "JOB" => StepType.Job,
                "EXE" => StepType.Exe,
                "PIPELINE" => StepType.Pipeline,
                "DATASET" => StepType.Dataset,
                "FUNCTION" => StepType.Function,
                _ => throw new ArgumentNullException(nameof(value), "StepType value cannot be null")
            };
            var stepTypeConverter = new ValueConverter<StepType, string>(v => stepTypeToString(v), v => stringToStepType(v));

            modelBuilder.HasDefaultSchema("etlmanager");

            Func<ExecutionStatus, string> executionStatusToString = value => value switch
            {
                ExecutionStatus.NotStarted => "NOT STARTED",
                ExecutionStatus.Running => "RUNNING",
                ExecutionStatus.Succeeded => "SUCCEEDED",
                ExecutionStatus.Failed => "FAILED",
                ExecutionStatus.Warning => "WARNING",
                ExecutionStatus.Stopped => "STOPPED",
                ExecutionStatus.Suspended => "SUSPENDED",
                _ => throw new ArgumentNullException(nameof(value), "ExecutionStatus value cannot be null")
            };
            Func<string, ExecutionStatus> stringToExecutionStatus = value => value switch
            {
                "NOT STARTED" => ExecutionStatus.NotStarted,
                "RUNNING" => ExecutionStatus.Running,
                "SUCCEEDED" => ExecutionStatus.Succeeded,
                "FAILED" => ExecutionStatus.Failed,
                "WARNING" => ExecutionStatus.Warning,
                "STOPPED" => ExecutionStatus.Stopped,
                "SUSPENDED" => ExecutionStatus.Suspended,
                _ => throw new ArgumentNullException(nameof(value), "ExecutionStatus value cannot be null")
            };
            var executionStatusConverter = new ValueConverter<ExecutionStatus, string>(v => executionStatusToString(v), v => stringToExecutionStatus(v));

            modelBuilder.Entity<Execution>()
                .ToTable("Execution")
                .Property(e => e.ExecutionStatus)
                .HasConversion(executionStatusConverter);

            modelBuilder.Entity<ExecutionParameter>()
                .ToTable("ExecutionParameter")
                .HasKey(p => new { p.ExecutionId, p.ParameterId });
            modelBuilder.Entity<ExecutionParameter>()
                .HasOne(p => p.Execution)
                .WithMany(e => e.ExecutionParameters);

            modelBuilder.Entity<StepExecution>()
                .ToTable("ExecutionStep")
                .HasKey(step => new { step.ExecutionId, step.StepId });
            modelBuilder.Entity<StepExecution>()
                .Property(e => e.StepType)
                .HasConversion(stepTypeConverter);
            modelBuilder.Entity<StepExecution>()
                .HasOne(step => step.Execution)
                .WithMany(e => e.StepExecutions);
            modelBuilder.Entity<StepExecution>()
                .HasDiscriminator<StepType>("StepType")
                .HasValue<DatasetStepExecution>(StepType.Dataset)
                .HasValue<ExeStepExecution>(StepType.Exe)
                .HasValue<JobStepExecution>(StepType.Job)
                .HasValue<PackageStepExecution>(StepType.Package)
                .HasValue<PipelineStepExecution>(StepType.Pipeline)
                .HasValue<SqlStepExecution>(StepType.Sql)
                .HasValue<FunctionStepExecution>(StepType.Function);

            Func<StepExecutionStatus, string> stepExecutionStatusToString = value => value switch
            {
                StepExecutionStatus.NotStarted => "NOT STARTED",
                StepExecutionStatus.Running => "RUNNING",
                StepExecutionStatus.Succeeded => "SUCCEEDED",
                StepExecutionStatus.Failed => "FAILED",
                StepExecutionStatus.Stopped => "STOPPED",
                StepExecutionStatus.Skipped => "SKIPPED",
                StepExecutionStatus.AwaitRetry => "AWAIT RETRY",
                StepExecutionStatus.Duplicate => "DUPLICATE",
                _ => throw new ArgumentNullException(nameof(value), "StepExecutionStatus value cannot be null")
            };
            Func<string, StepExecutionStatus> stringToStepExecutionStatus = value => value switch
            {
                "NOT STARTED" => StepExecutionStatus.NotStarted,
                "RUNNING" => StepExecutionStatus.Running,
                "SUCCEEDED" => StepExecutionStatus.Succeeded,
                "FAILED" => StepExecutionStatus.Failed,
                "STOPPED" => StepExecutionStatus.Stopped,
                "SKIPPED" => StepExecutionStatus.Skipped,
                "AWAIT RETRY" => StepExecutionStatus.AwaitRetry,
                "DUPLICATE" => StepExecutionStatus.Duplicate,
                _ => throw new ArgumentNullException(nameof(value), "StepExecutionStatus value cannot be null")
            };
            var stepExecutionStatusConverter = new ValueConverter<StepExecutionStatus, string>(v => stepExecutionStatusToString(v), v => stringToStepExecutionStatus(v));

            modelBuilder.Entity<StepExecutionAttempt>()
                .ToTable("ExecutionStepAttempt")
                .HasKey(sea => new { sea.ExecutionId, sea.StepId, sea.RetryAttemptIndex });
            modelBuilder.Entity<StepExecutionAttempt>()
                .Property(sea => sea.StepType)
                .HasConversion(stepTypeConverter);
            modelBuilder.Entity<StepExecutionAttempt>()
                .Property(sea => sea.ExecutionStatus)
                .HasConversion(stepExecutionStatusConverter);
            modelBuilder.Entity<StepExecutionAttempt>()
                .HasOne(sea => sea.StepExecution)
                .WithMany(step => step.StepExecutionAttempts);
            modelBuilder.Entity<StepExecutionAttempt>()
                .HasDiscriminator<StepType>("StepType")
                .HasValue<DatasetStepExecutionAttempt>(StepType.Dataset)
                .HasValue<ExeStepExecutionAttempt>(StepType.Exe)
                .HasValue<JobStepExecutionAttempt>(StepType.Job)
                .HasValue<PackageStepExecutionAttempt>(StepType.Package)
                .HasValue<PipelineStepExecutionAttempt>(StepType.Pipeline)
                .HasValue<SqlStepExecutionAttempt>(StepType.Sql)
                .HasValue<FunctionStepExecutionAttempt>(StepType.Function);

            modelBuilder.Entity<Dependency>()
                .ToTable("Dependency")
                .HasOne(dependency => dependency.Step)
                .WithMany(step => step.Dependencies)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Job>()
                .ToTable("Job")
                .HasMany(job => job.Steps)
                .WithOne(step => step.Job!);
            modelBuilder.Entity<Job>()
                .HasMany(job => job.Schedules)
                .WithOne(schedule => schedule.Job);
            modelBuilder.Entity<Job>()
                .HasMany(job => job.Subscriptions)
                .WithOne(subscription => subscription.Job);
            modelBuilder.Entity<Job>()
                .HasMany(j => j.Executions)
                .WithOne(e => e.Job!)
                .IsRequired(false);
            modelBuilder.Entity<Job>()
                .HasMany(job => job.JobParameters)
                .WithOne(param => param.Job);

            modelBuilder.Entity<Step>()
                .ToTable("Step")
                .HasOne(step => step.Job!)
                .WithMany(job => job.Steps)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Step>()
                .Property(s => s.StepType)
                .HasConversion(stepTypeConverter);
            modelBuilder.Entity<Step>()
                .HasDiscriminator<StepType>("StepType")
                .HasValue<DatasetStep>(StepType.Dataset)
                .HasValue<ExeStep>(StepType.Exe)
                .HasValue<JobStep>(StepType.Job)
                .HasValue<PackageStep>(StepType.Package)
                .HasValue<PipelineStep>(StepType.Pipeline)
                .HasValue<SqlStep>(StepType.Sql)
                .HasValue<FunctionStep>(StepType.Function);
            modelBuilder.Entity<JobStep>()
                .HasOne(step => step.JobToExecute)
                .WithMany(job => job.JobSteps)
                .HasForeignKey(step => step.JobToExecuteId);
            modelBuilder.Entity<Step>()
                .HasMany(s => s.StepExecutions)
                .WithOne(e => e.Step!)
                .IsRequired(false);

            modelBuilder.Entity<Tag>()
                .ToTable("Tag")
                .HasMany(t => t.Steps)
                .WithMany(s => s.Tags)
                .UsingEntity<Dictionary<string, object>>("StepTag",
                x => x.HasOne<Step>().WithMany().HasForeignKey("StepId"),
                x => x.HasOne<Tag>().WithMany().HasForeignKey("TagId"));

            modelBuilder.Entity<Schedule>()
                .ToTable("Schedule")
                .HasOne(schedule => schedule.Job)
                .WithMany(job => job.Schedules)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PackageParameter>(e =>
            {
                e.ToTable("PackageParameter")
                .HasOne(parameter => parameter.Step)
                .WithMany(step => step.PackageParameters)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<StepParameter>(e =>
            {
                e.ToTable("StepParameter")
                .HasOne(parameter => parameter.Step)
                .WithMany(step => step.StepParameters)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<StepExecutionParameter>()
                .ToTable("ExecutionStepParameter")
                .HasKey(param => new { param.ExecutionId, param.StepId, param.ParameterId });
            modelBuilder.Entity<StepExecutionParameter>()
                .HasOne(param => param.StepExecution)
                .WithMany(e => e.StepExecutionParameters)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<StepExecutionParameter>()
                .HasOne(p => p.ExecutionParameter)
                .WithMany(p => p!.StepExecutionParameters)
                .HasForeignKey(p => new { p.ExecutionId, p.ExecutionParameterId })
                .IsRequired(false);

            Func<SubscriptionType?, string?> subscriptionTypeToString = value => value switch
            {
                SubscriptionType.OnFailure => "FAILURE",
                SubscriptionType.OnSuccess => "SUCCESS",
                SubscriptionType.OnCompletion => "COMPLETION",
                _ => null
            };
            Func<string?, SubscriptionType?> stringToSubscriptionType = value => value switch
            {
                "FAILURE" => SubscriptionType.OnFailure,
                "SUCCESS" => SubscriptionType.OnSuccess,
                "COMPLETION" => SubscriptionType.OnCompletion,
                _ => null
            };
            var subscriptionTypeConverter = new ValueConverter<SubscriptionType?, string?>(v => subscriptionTypeToString(v), v => stringToSubscriptionType(v));

            modelBuilder.Entity<Subscription>()
                .ToTable("Subscription")
                .HasOne(subscription => subscription.Job)
                .WithMany(job => job.Subscriptions)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Subscription>()
                .HasOne(subscription => subscription.User)
                .WithMany(user => user.Subscriptions)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Subscription>()
                .Property(s => s.SubscriptionType)
                .HasConversion(subscriptionTypeConverter);

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

            modelBuilder.Entity<Connection>()
                .ToTable("Connection");

            modelBuilder.Entity<FunctionApp>()
                .ToTable("FunctionApp");
        }


        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            var user = HttpContext?.User?.Identity?.Name;

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
}
