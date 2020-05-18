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

        public DbSet<ExecutorManager.Models.Job> Job { get; set; }

        public DbSet<ExecutorManager.Models.Execution> Execution { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema("executor");
            modelBuilder.Entity<Execution>(builder =>
            {
                builder.ToTable("vExecution");
            });
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
        {
            // If there are Jobs or Steps that have been edited, set the LastModified date.
            var editedJobs = ChangeTracker.Entries().Where(entity => (entity.Entity is Job || entity.Entity is Step) && entity.State == EntityState.Modified).ToList();
            editedJobs.ForEach(entity => entity.Property("LastModifiedDateTime").CurrentValue = DateTime.Now);

            var addedJobs = ChangeTracker.Entries().Where(entity => (entity.Entity is Job || entity.Entity is Step) && entity.State == EntityState.Added).ToList();
            addedJobs.ForEach(entity => {
                entity.Property("CreatedDateTime").CurrentValue = DateTime.Now;
                entity.Property("LastModifiedDateTime").CurrentValue = DateTime.Now;
            });

            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        public DbSet<ExecutorManager.Models.Step> Step { get; set; }

    }
}
