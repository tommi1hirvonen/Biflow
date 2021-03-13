using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EtlManagerUi.Models;
using System.Threading;
using Microsoft.AspNetCore.Http;

namespace EtlManagerUi.Data
{
    public class EtlManagerContext : DbContext
    {
        private readonly HttpContext HttpContext;

        public EtlManagerContext (DbContextOptions<EtlManagerContext> options, IHttpContextAccessor httpContextAccessor)
            : base(options)
        {
            HttpContext = httpContextAccessor.HttpContext;
        }

        public DbSet<Job> Jobs { get; set; }
        public DbSet<Step> Steps { get; set; }
        public DbSet<StepExecution> Executions { get; set; }
        public DbSet<JobExecution> JobExecutions { get; set; }
        public DbSet<Dependency> Dependencies { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<RoleUser> EditableUsers { get; set; }
        public DbSet<DataFactory> DataFactories { get; set; }
        public DbSet<PowerBIService> PowerBIServices { get; set; }
        public DbSet<Connection> Connections { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Parameter> Parameters { get; set; }
        public DbSet<StepExecutionParameter> ExecutionParameters { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("etlmanager");
            
            // Map executions to views, which have additional logic implemented.
            // We never save or modify the executions via UI, so this is no problem.
            modelBuilder.Entity<StepExecution>()
                .ToView("vExecution");
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
                .WithOne(step => step.Job);
            modelBuilder.Entity<Job>()
                .HasMany(job => job.Schedules)
                .WithOne(schedule => schedule.Job);
            modelBuilder.Entity<Job>()
                .HasMany(job => job.Subscriptions)
                .WithOne(subscription => subscription.Job);
            
            modelBuilder.Entity<Step>()
                .ToTable("Step")
                .HasOne(step => step.Job)
                .WithMany(job => job.Steps)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

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
            
            modelBuilder.Entity<Parameter>()
                .ToTable("Parameter")
                .HasOne(parameter => parameter.Step)
                .WithMany(step => step.Parameters)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StepExecutionParameter>()
                .ToTable("vExecutionParameter")
                .HasOne(param => param.StepExecution)
                .WithMany(e => e.StepExecutionParameters)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
            
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
                .ToTable("DataFactory")
                .HasMany(df => df.Steps)
                .WithOne(step => step.DataFactory);

            modelBuilder.Entity<PowerBIService>()
                .ToTable("PowerBIService")
                .HasMany(df => df.Steps)
                .WithOne(step => step.PowerBIService);

            // Map Connection to a view, that has logic inside to hide encrypted connection strings from the UI.
            modelBuilder.Entity<Connection>()
                .ToTable("vConnection")
                .HasMany(connection => connection.Steps)
                .WithOne(step => step.Connection);
        }

        
        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            string user = HttpContext.User?.Identity?.Name;

            // Get new and modified steps.
            var stepEntities = ChangeTracker
                .Entries()
                .Where(entity => entity.Entity is Step && (entity.State == EntityState.Modified || entity.State == EntityState.Added))
                .ToList();

            // Reset data for steps that is not needed to prevent confusion in DB tables.
            foreach (var stepEntity in stepEntities)
            {
                Step step = (Step)stepEntity.Entity;
                if (step.StepType != "SQL")
                {
                    step.SqlStatement = null;
                }
                if (step.StepType != "SSIS")
                {
                    step.PackageFolderName = null;
                    step.PackageProjectName = null;
                    step.PackageName = null;
                }
                if (step.StepType != "SQL" && step.StepType != "SSIS")
                {
                    step.ConnectionId = null;
                }
                if (step.StepType != "PIPELINE")
                {
                    step.PipelineName = null;
                    step.DataFactoryId = null;
                }
                if (step.StepType != "JOB")
                {
                    step.JobToExecuteId = null;
                }
                if (step.StepType != "EXE")
                {
                    step.ExeFileName = null;
                    step.ExeArguments = null;
                    step.ExeSuccessExitCode = null;
                    step.ExeWorkingDirectory = null;
                }
                if (step.StepType != "DATASET")
                {
                    step.PowerBIServiceId = null;
                    step.DatasetGroupId = null;
                    step.DatasetId = null;
                }
            }


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
