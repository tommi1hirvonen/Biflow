using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EtlManagerExecutor
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(Directory.GetCurrentDirectory());
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .CreateLogger();

            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddTransient<IJobExecutor, JobExecutor>();
                    services.AddTransient<ISchedulesExecutor, SchedulesExecutor>();
                    services.AddTransient<IMailTest, MailTest>();
                })
                .UseSerilog()
                .Build();

            Parser.Default.ParseArguments<JobExecutorOptions, SchedulesExecutorOptions, MailTestOptions>(args)
                .MapResult(
                    (JobExecutorOptions options) => RunExecution(host, options),
                    (SchedulesExecutorOptions options) => RunSchedules(host, options),
                    (MailTestOptions options) => RunMailTest(host, options),
                    errors => HandleParseError(errors)
                );
            
        }

        static int RunExecution(IHost host, JobExecutorOptions options)
        {
            var service = ActivatorUtilities.CreateInstance<JobExecutor>(host.Services);
            service.Run(options.ExecutionId, options.Notify);
            return 0;
        }

        static int RunSchedules(IHost host, SchedulesExecutorOptions options)
        {
            var service = ActivatorUtilities.CreateInstance<SchedulesExecutor>(host.Services);
            service.Run(options.Time);
            return 0;
        }

        static int RunMailTest(IHost host, MailTestOptions options)
        {
            var service = ActivatorUtilities.CreateInstance<MailTest>(host.Services);
            service.Run(options.ToAddress);
            return 0;
        }

        static int HandleParseError(IEnumerable<Error> errors)
        {
            Log.Error("Error parsing command: " + string.Join("\n", errors.Select(error => error.ToString())));
            return 1;
        }

    }

    [Verb("execute", HelpText = "Start the execution of an initilized execution (execution rows have been addd to the Execution table).")]
    class JobExecutorOptions
    {
        [Value(0, HelpText = "Execution id", Required = true)]
        public string ExecutionId { get; set; }

        [Option("notify", Default = false, HelpText = "Notify subscribers with an email in case there were failed steps.", Required = false)]
        public bool Notify { get; set; }
    }

    [Verb("testmail", HelpText = "Send a test mail using email configuration from appsettings.json.")]
    class MailTestOptions
    {
        [Value(0, HelpText = "The address where the test email should be sent to", Required = true)]
        public string ToAddress { get; set; }
    }

    [Verb("execschedules", HelpText = "Execute schedules for a specific time of day.")]
    class SchedulesExecutorOptions
    {
        [Value(0, HelpText = "Time of day in format HHmm", Required = true)]
        public string Time { get; set; }
    }
}
