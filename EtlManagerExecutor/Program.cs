using CommandLine;
using EtlManagerDataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerExecutor
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                //.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .CreateLogger();

            var host = Host.CreateDefaultBuilder()
                .ConfigureHostConfiguration(configHost =>
                    configHost.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true))
                .ConfigureServices((context, services) =>
                {
                    services.AddTransient<IJobExecutor, JobExecutor>();
                    services.AddTransient<IExecutionStopper, ExecutionStopper>();
                    services.AddTransient<IMailTest, MailTest>();
                    var connectionString = context.Configuration.GetConnectionString("EtlManagerContext");
                    services.AddDbContextFactory<EtlManagerContext>(options => options.UseSqlServer(connectionString));
                })
                
                .UseSerilog()
                .Build();

            return await Parser.Default.ParseArguments<CommitOptions, JobExecutorOptions, CancelOptions, MailTestOptions>(args)
                .MapResult(
                    (JobExecutorOptions options) => RunExecutionAsync(host, options),
                    (CancelOptions options) => CancelExecutionAsync(host, options),
                    (MailTestOptions options) => RunMailTest(host, options),
                    (CommitOptions options) => PrintCommit(),
                    errors => HandleParseError(errors)
                );
        }

        static async Task<int> RunExecutionAsync(IHost host, JobExecutorOptions options)
        {
            var service = ActivatorUtilities.CreateInstance<JobExecutor>(host.Services);
            await service.RunAsync(options.ExecutionId, options.Notify);
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

        static async Task<int> RunMailTest(IHost host, MailTestOptions options)
        {
            var service = ActivatorUtilities.CreateInstance<MailTest>(host.Services);
            await service.RunAsync(options.ToAddress);
            return 0;
        }

        static async Task<int> HandleParseError(IEnumerable<Error> errors)
        {
            Log.Error("Error parsing command: " + string.Join("\n", errors.Select(error => error.ToString())));
            return await Task.FromResult(-1);
        }

        static async Task<int> PrintCommit()
        {
            var commit = Properties.Resources.CurrentCommit;
            Console.WriteLine(commit);
            return await Task.FromResult(0);
        }

    }

    [Verb("execute", HelpText = "Start the execution of an initilized execution (execution rows have been addd to the Execution table).")]
    class JobExecutorOptions
    {
        [Option('i', "id", HelpText = "Execution id", Required = true)]
        // Safe to suppress because Required = true
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string ExecutionId { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [Option('n', "notify", Default = false, HelpText = "Notify subscribers with an email in case there were failed steps.", Required = false)]
        public bool Notify { get; set; }
    }

    [Verb("test-mail", HelpText = "Send a test mail using email configuration from appsettings.json.")]
    class MailTestOptions
    {
        [Option('t', "send-to", HelpText = "The address where the test email should be sent to", Required = true)]
        // Safe to suppress because Required = true
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string ToAddress { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
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
}
