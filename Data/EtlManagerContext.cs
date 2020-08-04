using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EtlManager.Models;
using System.Threading;

namespace EtlManager.Data
{
    public class EtlManagerContext : DbContext
    {
        public EtlManagerContext (DbContextOptions<EtlManagerContext> options)
            : base(options)
        {
        }

        public DbSet<Job> Jobs { get; set; }
        public DbSet<Step> Steps { get; set; }
        public DbSet<StepExecution> Executions { get; set; }
        public DbSet<JobExecution> JobExecutions { get; set; }
        public DbSet<Dependency> Dependencies { get; set; }
        public DbSet<Schedule> Schedules { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("etlmanager");
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
            modelBuilder.Entity<Step>()
                .ToTable("Step")
                .HasOne(step => step.Job)
                .WithMany(job => job.Steps)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Schedule>()
                .ToTable("Schedule")
                .HasOne(schedule => schedule.Job)
                .WithMany(job => job.Schedules)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        }

        
        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            // If there are Jobs or Steps that have been edited, set the LastModified date.
            var editedJobs = ChangeTracker.Entries().Where(entity => (entity.Entity is Job || entity.Entity is Step) && entity.State == EntityState.Modified).ToList();
            editedJobs.ForEach(entity => entity.Property("LastModifiedDateTime").CurrentValue = DateTime.Now);

            var addedJobs = ChangeTracker.Entries().Where(entity => (entity.Entity is Job || entity.Entity is Step) && entity.State == EntityState.Added).ToList();
            addedJobs.ForEach(entity =>
            {
                entity.Property("CreatedDateTime").CurrentValue = DateTime.Now;
                entity.Property("LastModifiedDateTime").CurrentValue = DateTime.Now;
            });

            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        

    }
}
