using System.Text.Json.Serialization;
using Biflow.Proxy.Core;
using Biflow.Proxy.Core.Authentication;
using Biflow.Proxy.WebApp;
using Biflow.Proxy.WebApp.Endpoints;
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
    .AddSingleton<TasksRunner<ExeProxyRunResult>>()
    .ConfigureHttpJsonOptions(options =>
        options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default))
    .Configure<RouteOptions>(options =>
        options.SetParameterPolicy<RegexInlineRouteConstraint>("regex"));

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