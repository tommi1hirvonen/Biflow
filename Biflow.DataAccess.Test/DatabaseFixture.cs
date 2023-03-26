using Biflow.DataAccess.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Respawn;
using Xunit;

namespace Biflow.DataAccess.Test;

public class DatabaseFixture : IAsyncLifetime
{
    public string ConnectionString { get; } =
        "Data Source=localhost;Database=BiflowTest;Integrated Security=sspi;Encrypt=true;TrustServerCertificate=true;";

    public string Username { get; } = "testuser";

    public string Role { get; } = "Admin";

    public IDbContextFactory<BiflowContext> DbContextFactory { get; }

    public DatabaseFixture()
    {
        var httpContextAccessor = new MockHttpContextAccessor(Username, Role);
        var settings = new Dictionary<string, string?>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton<IHttpContextAccessor>(httpContextAccessor)
            .AddDbContextFactory<BiflowContext>(options =>
            {
                options.EnableSensitiveDataLogging();
                options.UseSqlServer(ConnectionString, o =>
                {
                    o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                });
            })
            .BuildServiceProvider();
        var factory = services.GetService<IDbContextFactory<BiflowContext>>();
        ArgumentNullException.ThrowIfNull(factory);
        DbContextFactory = factory;
    }

    public async Task InitializeAsync()
    {
        var respawner = await Respawner.CreateAsync(ConnectionString);
        await respawner.ResetAsync(ConnectionString);

        // Initialize seed data
        var context = await DbContextFactory.CreateDbContextAsync();
        var job = new Job
        {
            JobName = "Test job",
            JobDescription = "Test job",
            UseDependencyMode = true,
            StopOnFirstError = true,
            MaxParallelSteps = 4,
            OvertimeNotificationLimitMinutes = 120,
            IsEnabled = true,
            Category = new JobCategory { CategoryName = "Test category" }
        };
        context.AddRange(job);
        await context.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
