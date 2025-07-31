using System.Diagnostics;
using Biflow.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Aspire.MigrationService;

public class Worker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime hostApplicationLifetime) : BackgroundService
{
    public const string ActivitySourceName = "Migrations";

    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("Migrating database", ActivityKind.Client);
        
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            
            await RunMigrationAsync(dbContext, cancellationToken);
            // await SeedDataAsync(dbContext, cancellationToken);
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            throw;
        }

        hostApplicationLifetime.StopApplication();
    }

    private static async Task RunMigrationAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Run migration in a transaction to avoid partial migration if it fails.
            await dbContext.Database.MigrateAsync(cancellationToken);
        });
    }

    // private static async Task SeedDataAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    // {
    //     var firstJob = new Job
    //     {
    //         JobName = "Test Job"
    //     };
    //
    //     var strategy = dbContext.Database.CreateExecutionStrategy();
    //     await strategy.ExecuteAsync(async () =>
    //     {
    //         await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
    //         await dbContext.Jobs.AddAsync(firstJob, cancellationToken);
    //         await dbContext.SaveChangesAsync(cancellationToken);
    //         await transaction.CommitAsync(cancellationToken);
    //     });
    // }
}