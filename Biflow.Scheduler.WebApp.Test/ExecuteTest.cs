using Biflow.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Quartz;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Biflow.Scheduler.WebApp.Test;

[TestClass]
public class ExecuteTest
{
    [TestMethod]
    public async Task TestExecute()
    {
        var connectionString = "Server=localhost;Database=Biflow;Integrated Security=true;MultipleActiveResultSets=true;TrustServerCertificate=true;";

        var settings = new Dictionary<string, string?>
        {
            { "ConnectionStrings:AppDbContext", connectionString }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddLogging()
            .AddDbContextFactory<AppDbContext>(options =>
                    options.UseSqlServer(connectionString, o =>
                        o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)))
            .AddSingleton<WebAppExecutionJob>()
            .BuildServiceProvider();

        var executionJob = services.GetService<WebAppExecutionJob>();

        var jobId = Guid.Parse("9e337948-1cd4-4096-06c6-08da43c469bd");
        var scheduleId = Guid.Parse("b4581c87-3d92-4841-2809-08daed9b68ee");
        var jobContext = new MockJobContext(jobId, scheduleId);

        ArgumentNullException.ThrowIfNull(executionJob);

        await executionJob.Execute(jobContext);
    }
}