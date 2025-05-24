using System.Text.Json.Serialization;
using Biflow.ExecutorProxy.Core;
using Biflow.ExecutorProxy.Core.Authentication;
using Biflow.ExecutorProxy.Core.FilesExplorer;
using Biflow.ExecutorProxy.WebApp;
using Biflow.ExecutorProxy.WebApp.Endpoints;
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
    .AddSingleton<ExeTasksRunner>()
    .AddHostedService(s =>
        s.GetRequiredService<ExeTasksRunner>())
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
baseGroup.MapFileExplorerEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/health", () => Results.Text("Healthy")); // Skip authentication for health checks in development.
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // MapDefaultEndpoints() adds health check endpoint when running in development mode.
    // In production, add health check endpoint manually with auth enabled (baseGroup).
    baseGroup.MapGet("/health", () => Results.Text("Healthy"));
}

app.Run();


// JsonSerializable attributes together with JsonSerializerContext enable
// source generation for JSON serialization.

[JsonSerializable(typeof(ExeProxyRunRequest))]
[JsonSerializable(typeof(ExeTaskStatusResponse))]
[JsonSerializable(typeof(TaskStartedResponse))]
[JsonSerializable(typeof(FileExplorerSearchRequest))]
[JsonSerializable(typeof(FileExplorerSearchResponse))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;