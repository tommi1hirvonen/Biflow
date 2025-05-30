﻿using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace Biflow.Executor.Core;

public class ExecutorDbContext(IConfiguration configuration) : AppDbContext(configuration, userService: null)
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

        modelBuilder.Entity<Execution>()
            .HasQueryFilter(null);
    }
}
