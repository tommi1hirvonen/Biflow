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
            modelBuilder.HasDefaultSchema("etlmanager");

            var executionStatusConverter = new EnumToStringConverter<ExecutionStatus>();

            modelBuilder.Entity<Execution>()
                .ToTable("Execution")
                .Property(e => e.ExecutionStatus)
                .HasConversion(executionStatusConverter);

            var parameterTypeConverter = new EnumToStringConverter<ParameterType>();

            modelBuilder.Entity<ExecutionParameter>(e =>
            {
                e.ToTable("ExecutionParameter")
                .HasKey(p => new { p.ExecutionId, p.ParameterId });
                e.HasOne(p => p.Execution)
                .WithMany(e => e.ExecutionParameters);
                e.Property(p => p.ParameterType).HasConversion(parameterTypeConverter);
            });

            var stepTypeConverter = new EnumToStringConverter<StepType>();

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
                .HasValue<FunctionStepExecution>(StepType.Function);
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
                .HasValue<FunctionStepExecutionAttempt>(StepType.Function);
            });

            modelBuilder.Entity<Dependency>()
                .ToTable("Dependency")
                .HasOne(dependency => dependency.Step)
                .WithMany(step => step.Dependencies)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

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
            }); 

            modelBuilder.Entity<JobParameter>()
                .Property(p => p.ParameterType)
                .HasConversion(parameterTypeConverter);

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
                .HasValue<FunctionStep>(StepType.Function);
                e.HasMany(s => s.StepExecutions)
                .WithOne(e => e.Step!)
                .IsRequired(false);
            });

            modelBuilder.Entity<JobStep>()
                .HasOne(step => step.JobToExecute)
                .WithMany(job => job.JobSteps)
                .HasForeignKey(step => step.JobToExecuteId);

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

            var parameterLevelConverter = new EnumToStringConverter<ParameterLevel>();

            modelBuilder.Entity<PackageParameter>(e =>
            {
                e.ToTable("PackageParameter")
                .HasOne(parameter => parameter.Step)
                .WithMany(step => step.PackageParameters)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
                e.Property(p => p.ParameterType).HasConversion(parameterTypeConverter);
                e.Property(p => p.ParameterLevel).HasConversion(parameterLevelConverter);
            });

            modelBuilder.Entity<StepParameter>(e =>
            {
                e.ToTable("StepParameter")
                .HasOne(parameter => parameter.Step)
                .WithMany(step => step.StepParameters)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
                e.Property(p => p.ParameterType).HasConversion(parameterTypeConverter);
            });

            modelBuilder.Entity<StepExecutionParameter>(e =>
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
                e.Property(p => p.ParameterType).HasConversion(parameterTypeConverter);
                e.Property(p => p.ParameterLevel).HasConversion(parameterLevelConverter);
            });

            var subscriptionTypeConverter = new EnumToStringConverter<SubscriptionType>();

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
