using System.Text.Json.Serialization;
using Biflow.Proxy.Core;
using Biflow.Proxy.Core.Authentication;
using Biflow.Proxy.WebApp;
using Biflow.Proxy.WebApp.Endpoints;
using Biflow.Proxy.WebApp.ProxyTasks;
using Microsoft.AspNetCore.Routing.Constraints;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateSlimBuilder(args);

Serilog.Debugging.SelfLog.Enable(Console.Error);

builder.WebHost.UseKestrelHttpsConfiguration();
builder.AddServiceDefaults();

if (builder.Configuration.GetValue<string?>("LogFilePath") is { } logFilePath)
{
    var logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .WriteTo.File(logFilePath,
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:HH:mm:ss.fff zzz} [{Level:w3}] {Message:lj}{NewLine}{Exception}")
        .CreateLogger();
    builder.Logging.AddSerilog(logger, dispose: true);
}

builder.Services.AddWindowsService()
    .AddSystemd()
    .AddEndpointsApiExplorer()
    .AddSwagger()
    .AddSingleton<TasksRunner<ExeProxyTask, ExeTaskRunningStatusResponse, ExeProxyRunResult>>()
    .AddHostedService(s =>
        s.GetRequiredService<TasksRunner<ExeProxyTask, ExeTaskRunningStatusResponse, ExeProxyRunResult>>())
    .ConfigureHttpJsonOptions(options =>
        options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default))
    .Configure<RouteOptions>(options =>
        options.SetParameterPolicy<RegexInlineRouteConstraint>("regex"))
    // Timeout for hosted services to shut down gracefully when StopAsync() is called.
    .Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(20));

var app = builder.Build();

app.UseExceptionHandler();

var baseGroup = app.MapGroup("").AddEndpointFilter<ServiceApiKeyEndpointFilter>();

baseGroup.MapExeEndpoints();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();

[JsonSerializable(typeof(ExeProxyRunResult))]
[JsonSerializable(typeof(ExeProxyRunRequest))]
[JsonSerializable(typeof(ExeTaskStatusResponse))]
[JsonSerializable(typeof(TaskStartedResponse))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;