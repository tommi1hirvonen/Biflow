using CommandLine;
using EtlManager.DataAccess;
using EtlManager.DataAccess.Models;
using EtlManager.Executor.ConsoleApp.ExecutionStopper;
using EtlManager.Executor.Core.Common;
using EtlManager.Executor.Core.ConnectionTest;
using EtlManager.Executor.Core.JobExecutor;
using EtlManager.Executor.Core.Notification;
using EtlManager.Executor.Core.Orchestrator;
using EtlManager.Executor.Core.StepExecutor;
using EtlManager.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .CreateLogger();

var builder = Host.CreateDefaultBuilder();

builder.ConfigureHostConfiguration(configHost =>
    configHost.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true));

builder.ConfigureServices((context, services) =>
{
    var connectionString = context.Configuration.GetConnectionString("EtlManagerContext");
    services.AddDbContextFactory<EtlManagerContext>(options =>
        options.UseSqlServer(connectionString, o =>
            o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

    services.AddHttpClient();
    services.AddHttpClient("notimeout", client => client.Timeout = Timeout.InfiniteTimeSpan);
    services.AddSingleton<ITokenService, TokenService>();
    services.AddSingleton<IExecutionConfiguration, ExecutionConfiguration>();
    services.AddSingleton<IEmailConfiguration, EmailConfiguration>();
    services.AddSingleton<INotificationService, EmailService>();
    services.AddSingleton<IStepExecutorFactory, StepExecutorFactory>();
    services.AddSingleton<IOrchestratorFactory, OrchestratorFactory>();
    services.AddSingleton<IExecutionStopper, ExecutionStopper>();
    services.AddSingleton<IEmailTest, EmailTest>();
    services.AddSingleton<IConnectionTest, ConnectionTest>();
    services.AddTransient<IJobExecutor, JobExecutor>();
});

builder.UseSerilog();

var host = builder.Build();

return await Parser.Default
    .ParseArguments<CommitOptions, JobExecutorOptions, CancelOptions, EmailTestOptions, ConnectionTestOptions>(args)
    .MapResult(
        (JobExecutorOptions options) => RunExecutionAsync(host, options),
        (CancelOptions options) => CancelExecutionAsync(host, options),
        (EmailTestOptions options) => RunEmailTest(host, options),
        (ConnectionTestOptions options) => RunConnectionTest(host),
        (CommitOptions options) => PrintCommit(),
        errors => HandleParseError(errors)
    );

async Task<int> RunExecutionAsync(IHost host, JobExecutorOptions options)
{
    var executor = ActivatorUtilities.CreateInstance<JobExecutor>(host.Services);
    _ = Task.Run(() => ReadCancelKey(executor));
    _ = Task.Run(() => ReadCancelPipe(executor, options.ExecutionId));
    await executor.RunAsync(options.ExecutionId, options.Notify, options.NotifyMe, options.NotifyMeOvertime);
    return 0;
}

static async Task<int> CancelExecutionAsync(IHost host, CancelOptions options)
{
    var service = ActivatorUtilities.CreateInstance<ExecutionStopper>(host.Services);
    try
    {
        var result = await service.RunAsync(options.ExecutionId, options.Username, options.StepId);
        return result ? 0 : -1;
    }
    catch (Exception)
    {
        return -1;
    }
}

static async Task<int> RunEmailTest(IHost host, EmailTestOptions options)
{
    var service = ActivatorUtilities.CreateInstance<EmailTest>(host.Services);
    await service.RunAsync(options.ToAddress);
    return 0;
}

static async Task<int> RunConnectionTest(IHost host)
{
    var service = ActivatorUtilities.CreateInstance<ConnectionTest>(host.Services);
    await service.RunAsync();
    return 0;
}

static async Task<int> HandleParseError(IEnumerable<Error> errors)
{
    Log.Error("Error parsing command: " + string.Join("\n", errors.Select(error => error.ToString())));
    return await Task.FromResult(-1);
}

static async Task<int> PrintCommit()
{
    var commit = EtlManager.Executor.ConsoleApp.Properties.Resources.CurrentCommit;
    Console.WriteLine(commit);
    return await Task.FromResult(0);
}

void ReadCancelKey(IJobExecutor jobExecutor)
{
    Console.WriteLine("Enter 'c' to cancel all step executions or a step id to cancel that step's execution.");
    while (true)
    {
        var input = Console.ReadLine();
        try
        {
            ProcessCancelInput(input, jobExecutor);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error canceling execution: {ex.Message}");
        }
    }
}

void ProcessCancelInput(string? input, IJobExecutor jobExecutor)
{
    if (input == "c")
    {
        Console.WriteLine("Canceling all step executions.");
        jobExecutor.Cancel("console");
    }
    else if (input is not null)
    {
        var stepId = Guid.Parse(input);
        jobExecutor.Cancel("console", stepId);
    }
}


void ReadCancelPipe(IJobExecutor jobExecutor, Guid executionId)
{
    while (true)
    {
        using var pipeServer = new NamedPipeServerStream(executionId.ToString().ToLower(), PipeDirection.In);
        pipeServer.WaitForConnection();
        try
        {
            using var streamReader = new StreamReader(pipeServer);
            var builder = new StringBuilder();
            string? input;
            while ((input = streamReader.ReadLine()) is not null)
            {
                builder.Append(input);
            }
            var json = builder.ToString();
            var cancelCommand = JsonSerializer.Deserialize<CancelCommand>(json)
                ?? throw new ArgumentNullException("cancelCommand", "Cancel command cannot be null");
            if (cancelCommand.StepId is not null)
            {
                // Cancel just one step
                jobExecutor.Cancel(cancelCommand.Username, (Guid)cancelCommand.StepId);
            }
            else
            {
                // Cancel all steps
                jobExecutor.Cancel(cancelCommand.Username);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error canceling execution");
        }
    }
}

[Verb("execute", HelpText = "Start the execution of an initialized execution (execution placeholder created in database).")]
class JobExecutorOptions
{
    [Option('i', "id", HelpText = "Execution id", Required = true)]
    public Guid ExecutionId { get; set; }

    [Option('n', "notify", Default = false, HelpText = "Notify subscribers with an email based on their subscription.", Required = false)]
    public bool Notify { get; set; }

    [Option("notify-me",
        Default = null,
        HelpText = "Notify me with an email after the execution has finished. Possible values are null (omitted), OnCompletion, OnFailure, OnSuccess",
        Required = false)]
    public SubscriptionType? NotifyMe { get; set; }

    [Option("notify-me-overtime", Default = false, HelpText = "Notify me with an email if the execution exceeds the overtime limit set for the job.", Required = false)]
    public bool NotifyMeOvertime { get; set; }
}

[Verb("test-email", HelpText = "Send a test email using email configuration from appsettings.json.")]
class EmailTestOptions
{
    [Option('t', "send-to", HelpText = "The address where the test email should be sent to", Required = true)]
    // Safe to suppress because Required = true
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public string ToAddress { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}

[Verb("test-connection", HelpText = "Test connection to database defined in appsettings.json.")]
class ConnectionTestOptions
{
}

[Verb("cancel", HelpText = "Cancel a running execution under a different executor process.")]
class CancelOptions
{
    [Option('i', "execution-id", HelpText = "Execution id", Required = true)]
    // Safe to suppress because Required = true
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public string ExecutionId { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [Option('u', "username", HelpText = "Username for the user who initiated the cancel operation.", Required = false)]
    public string? Username { get; set; }

    [Option('s', "step-id", HelpText = "Step id for a specific step that should be canceled (optional).", Required = false)]
    public string? StepId { get; set; }
}


[Verb("get-commit", HelpText = "Return the current version's Git commit checksum.")]
class CommitOptions
{
}