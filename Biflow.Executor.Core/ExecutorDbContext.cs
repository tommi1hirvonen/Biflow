using Biflow.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace Biflow.Executor.Core;

internal class ExecutorDbContext : AppDbContext
{
    public ExecutorDbContext(IConfiguration configuration)
        : base(configuration, httpContextAccessor: null) // Pass null as HttpContextAccessor to disable global query filters.
    {
    }

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
}
