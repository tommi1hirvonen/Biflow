using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ExecutorManager.Models;
using System.Threading;

namespace ExecutorManager.Data
{
    public class ExecutorManagerContext : DbContext
    {
        public ExecutorManagerContext (DbContextOptions<ExecutorManagerContext> options)
            : base(options)
        {
        }

        public DbSet<Job> Jobs { get; set; }

        public DbSet<Step> Steps { get; set; }

        public DbSet<Execution> Executions { get; set; }

        public DbSet<Dependency> Dependencies { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("executor");
            modelBuilder.Entity<Execution>()
                .ToView("vExecution");
            modelBuilder.Entity<Dependency>()
                .ToView("vDependency");
            modelBuilder.Entity<Job>()
                .ToTable("Job")
                .HasMany(job => job.Steps)
                .WithOne(step => step.Job);
            modelBuilder.Entity<Step>()
                .ToTable("Step")
                .HasOne(step => step.Job)
                .WithMany(job => job.Steps)
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
