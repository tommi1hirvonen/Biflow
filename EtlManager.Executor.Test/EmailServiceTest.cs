using EtlManager.DataAccess;
using EtlManager.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EtlManager.Executor.Test;

[TestClass]
public class EmailServiceTest
{
    [TestMethod]
    public async Task TestEmailService()
    {
        var settings = new Dictionary<string, string>
        {
            { "EmailSettings:SmtpServer", "" },
            { "EmailSettings:EnableSsl", "" },
            { "EmailSettings:Port", "" },
            { "EmailSettings:FromAddress", "" },
            { "EmailSettings:AnonymousAuthentication", "" },
            { "EmailSettings:Username", "" },
            { "EmailSettings:Password", "" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var connectionString = "Server=localhost;Database=EtlManager;Integrated Security=true;MultipleActiveResultSets=true";

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddDbContextFactory<EtlManagerContext>(options =>
                    options.UseSqlServer(connectionString, o =>
                        o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)))
            .AddSingleton<IEmailConfiguration, EmailConfiguration>()
            .AddSingleton<INotificationService, EmailService>()
            .BuildServiceProvider();

        var dbContextFactory = services.GetService<IDbContextFactory<EtlManagerContext>>();
        Assert.IsNotNull(dbContextFactory);
        using var context = dbContextFactory.CreateDbContext();

        var execution = await context.Executions
                    .AsNoTrackingWithIdentityResolution()
                    .Include(e => e.Job)
                    .Include(e => e.ExecutionParameters)
                    .Include(e => e.StepExecutions)
                    .ThenInclude(e => e.StepExecutionAttempts)
                    .Include(e => e.StepExecutions)
                    .ThenInclude(e => e.ExecutionDependencies)
                    .ThenInclude(e => e.DependantOnStepExecution)
                    .Include(e => e.StepExecutions)
                    .ThenInclude(e => (e as ParameterizedStepExecution)!.StepExecutionParameters)
                    .ThenInclude(p => p.ExecutionParameter)
                    .Include(e => e.StepExecutions)
                    .ThenInclude(e => (e as DatasetStepExecution)!.AppRegistration)
                    .Include(e => e.StepExecutions)
                    .ThenInclude(e => (e as FunctionStepExecution)!.FunctionApp)
                    .Include(e => e.StepExecutions)
                    .ThenInclude(e => (e as PipelineStepExecution)!.DataFactory)
                    .ThenInclude(df => df.AppRegistration)
                    .Include(e => e.StepExecutions)
                    .ThenInclude(e => (e as SqlStepExecution)!.Connection)
                    .Include(e => e.StepExecutions)
                    .ThenInclude(e => (e as PackageStepExecution)!.Connection)
                    .Include(e => e.StepExecutions)
                    .ThenInclude(e => (e as AgentJobStepExecution)!.Connection)
                    .Include(e => e.StepExecutions)
                    .ThenInclude(e => (e as TabularStepExecution)!.Connection)
                    .FirstAsync(e => e.ExecutionId == Guid.Parse("92d8eae5-b4aa-4b53-9c4f-d5fbec913515"));

        var emailService = services.GetService<INotificationService>();
        Assert.IsNotNull(emailService);
        await emailService.SendCompletionNotification(execution);
    }
}
