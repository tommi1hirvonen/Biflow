using Microsoft.Extensions.Configuration;

namespace Biflow.Ui.Core;

public class RevertDbContext(IConfiguration configuration) : AppDbContext(configuration, userService: null)
{
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

    protected override void OnSavingChanges(object? sender, SavingChangesEventArgs e)
    {
        // Skip updating audit fields when doing version revert.
    }
}