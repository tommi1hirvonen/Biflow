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
        public DbSet<StepExecution> Executions => Set<StepExecution>();
        public DbSet<JobExecution> JobExecutions => Set<JobExecution>();
        public DbSet<Dependency> Dependencies => Set<Dependency>();
        public DbSet<Schedule> Schedules => Set<Schedule>();
        public DbSet<Subscription> Subscriptions => Set<Subscription>();
        public DbSet<User> Users => Set<User>();
        public DbSet<RoleUser> EditableUsers => Set<RoleUser>();
        public DbSet<DataFactory> DataFactories => Set<DataFactory>();
        public DbSet<PowerBIService> PowerBIServices => Set<PowerBIService>();
        public DbSet<Connection> Connections => Set<Connection>();
        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<PackageParameter> PackageParameters => Set<PackageParameter>();
        public DbSet<PipelineParameter> PipelineParameters => Set<PipelineParameter>();
        public DbSet<StepExecutionParameter> ExecutionParameters => Set<StepExecutionParameter>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Func<StepType?, string?> stepTypeToString = value => value switch
            {
                StepType.Sql => "SQL",
                StepType.Package => "SSIS",
                StepType.Job => "JOB",
                StepType.Exe => "EXE",
                StepType.Pipeline => "PIPELINE",
                StepType.Dataset => "DATASET",
                _ => null
            };
            Func<string?, StepType?> stringToStepType = value => value switch
            {
                "SQL" => StepType.Sql,
                "SSIS" => StepType.Package,
                "JOB" => StepType.Job,
                "EXE" => StepType.Exe,
                "PIPELINE" => StepType.Pipeline,
                "DATASET" => StepType.Dataset,
                _ => null
            };
            var stepTypeConverter = new ValueConverter<StepType?, string?>(v => stepTypeToString(v), v => stringToStepType(v));

            modelBuilder.HasDefaultSchema("etlmanager");

            // Map executions to views, which have additional logic implemented.
            // We never save or modify the executions via UI, so this is no problem.
            modelBuilder.Entity<StepExecution>()
                .ToView("vExecution")
                .Property(e => e.StepType)
                .HasConversion(stepTypeConverter);
            modelBuilder.Entity<JobExecution>()
                .ToView("vExecutionJob");

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
                .HasDiscriminator<StepType?>("StepType")
                .HasValue<DatasetStep>(StepType.Dataset)
                .HasValue<ExeStep>(StepType.Exe)
                .HasValue<JobStep>(StepType.Job)
                .HasValue<PackageStep>(StepType.Package)
                .HasValue<PipelineStep>(StepType.Pipeline)
                .HasValue<SqlStep>(StepType.Sql);

            modelBuilder.Entity<Tag>()
                .ToTable("Tag")
                .HasMany(t => t.Steps)
                .WithMany(s => s.Tags)
                .UsingEntity(e => e.ToTable("StepTag"));

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

            modelBuilder.Entity<PipelineParameter>(e =>
            {
                e.ToTable("PipelineParameter")
                .HasOne(parameter => parameter.Step)
                .WithMany(step => step.PipelineParameters)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<StepExecutionParameter>()
                .ToTable("vExecutionParameter")
                .HasOne(param => param.StepExecution)
                .WithMany(e => e.StepExecutionParameters)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

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

            // Map a secondary user class RoleUser to an additional vUser view that is based on User table.
            // We cannot map two entity types (i.e. User and RoleUser) to the same table, which is why this is needed.

            // The reason for separating User and RoleUser is a legacy one from the times of the Razor Pages app.
            // There we couldn't expose RoleUser to the end user in HTML forms, because the user may have been able to edit their own role.
            modelBuilder.Entity<RoleUser>()
                .ToTable("vUser");

            modelBuilder.Entity<DataFactory>()
                .ToTable("vDataFactory");

            modelBuilder.Entity<PowerBIService>()
                .ToTable("vPowerBIService");

            // Map Connection to a view, that has logic inside to hide encrypted connection strings from the UI.
            modelBuilder.Entity<Connection>()
                .ToTable("vConnection");
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
                entity.Property("LastModifiedDateTime").CurrentValue = DateTime.Now;
                entity.Property("LastModifiedBy").CurrentValue = user;
            });

            // If there are Jobs or Steps that have been added, set the audit fields.
            var addedJobsAndSteps = ChangeTracker.Entries().Where(entity => (entity.Entity is Job || entity.Entity is Step) && entity.State == EntityState.Added).ToList();
            addedJobsAndSteps.ForEach(entity =>
            {
                entity.Property("CreatedDateTime").CurrentValue = DateTime.Now;
                entity.Property("LastModifiedDateTime").CurrentValue = DateTime.Now;
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
                entity.Property("CreatedDateTime").CurrentValue = DateTime.Now;
                entity.Property("CreatedBy").CurrentValue = user;
            });

            // Set the audit fields for edited users.
            var editedUsers = ChangeTracker.Entries().Where(entity => entity.Entity is User && entity.State == EntityState.Modified).ToList();
            editedUsers.ForEach(user => user.Property("LastModifiedDateTime").CurrentValue = DateTime.Now);

            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }


    }
}
