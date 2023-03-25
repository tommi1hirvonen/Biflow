using CommandLine;
using Biflow.Executor.ConsoleApp;
using Biflow.Executor.ConsoleApp.ExecutionStopper;
using Biflow.Executor.Core;
using Biflow.Executor.Core.ConnectionTest;
using Biflow.Executor.Core.JobExecutor;
using Biflow.Executor.Core.Notification;
using Biflow.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.IO.Pipes;
using System.Reflection;
using System.Text;
using System.Text.Json;

var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
ArgumentException.ThrowIfNullOrEmpty(assemblyPath);

var host = Host.CreateDefaultBuilder(args)
    .UseContentRoot(assemblyPath) // Force console app to read appsettings from its own folder (instead of launching UI app's folder).
    .ConfigureHostConfiguration(config => config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true))
    .ConfigureLogging((context, config) =>
    {
        var logger = new LoggerConfiguration().ReadFrom.Configuration(context.Configuration).CreateLogger();
        config.AddSerilog(logger, dispose: true);
    })
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration.GetConnectionString("BiflowContext");
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        services.AddExecutorServices<ExecutorLauncher>(connectionString);
        services.AddSingleton<IExecutionStopper, ExecutionStopper>();
    })
    .Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();

return await Parser.Default
    .ParseArguments<CommitOptions, JobExecutorOptions, CancelOptions, EmailTestOptions, ConnectionTestOptions>(args)
    .MapResult(
        (JobExecutorOptions options) => RunExecutionAsync(host, options),
        (CancelOptions options) => CancelExecutionAsync(host, options),
        (EmailTestOptions options) => RunEmailTest(host, options),
        (ConnectionTestOptions options) => RunConnectionTest(host),
        (CommitOptions options) => PrintCommit(),
        errors => HandleParseError(logger, errors)
    );

async Task<int> RunExecutionAsync(IHost host, JobExecutorOptions options)
{
    var factory = host.Services.GetRequiredService<IJobExecutorFactory>();
    var executor = await factory.CreateAsync(options.ExecutionId);
    _ = Task.Run(() => ReadCancelKey(executor));
    _ = Task.Run(() => ReadCancelPipe(executor, options.ExecutionId));
    await executor.RunAsync(options.ExecutionId);
    return 0;
}

static async Task<int> CancelExecutionAsync(IHost host, CancelOptions options)
{
    var service = host.Services.GetRequiredService<IExecutionStopper>();
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
    var service = host.Services.GetRequiredService<IEmailTest>();
    await service.RunAsync(options.ToAddress);
    return 0;
}

static async Task<int> RunConnectionTest(IHost host)
{
    var service = host.Services.GetRequiredService<IConnectionTest>();
    await service.RunAsync();
    return 0;
}

static async Task<int> HandleParseError(ILogger<Program> logger, IEnumerable<Error> errors)
{
    logger.LogError("Error parsing command: {cmd}", string.Join("\n", errors.Select(error => error.ToString())));
    return await Task.FromResult(-1);
}

static async Task<int> PrintCommit()
{
    var commit = Biflow.Executor.ConsoleApp.Properties.Resources.CurrentCommit;
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
            logger.LogError(ex, "Error canceling execution");
        }
    }
}

[Verb("execute", HelpText = "Start the execution of an initialized execution (execution placeholder created in database).")]
class JobExecutorOptions
{
    [Option('i', "id", HelpText = "Execution id", Required = true)]
    public Guid ExecutionId { get; set; }
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