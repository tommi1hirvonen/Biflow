using Biflow.Core.Entities;
using Biflow.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace Biflow.Scheduler.Core;

public class SchedulerDbContext(IConfiguration configuration) : AppDbContext(configuration, userService: null)
{
    protected override void ConfigureSqlServer(SqlServerDbContextOptionsBuilder options)
    {
        base.ConfigureSqlServer(options);
        options.EnableRetryOnFailure();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.EnableSensitiveDataLogging();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Override and remove base class query filters on jobs and executions.
        // This results in simpler and more performant queries.

        modelBuilder.Entity<Job>()
            .HasQueryFilter(null);
    }
}
