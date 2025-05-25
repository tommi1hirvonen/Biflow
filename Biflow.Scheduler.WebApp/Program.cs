using Biflow.Core;
using Biflow.Executor.Core;
using Biflow.ExecutorProxy.Core.Authentication;
using Biflow.Scheduler.Core;
using Biflow.Scheduler.WebApp;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults(addDbHealthCheck: true);

builder.Services.AddWindowsService();
builder.Services.AddSystemd();

if (builder.Configuration.GetSection("Serilog").Exists())
{
    var logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
    builder.Logging.AddSerilog(logger, dispose: true);
}

builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(s =>
{
    s.SwaggerDoc("v1", new OpenApiInfo { Title = "Biflow Scheduler API", Version = "v1" });
    s.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "The API to authenticate with the API",
        Type = SecuritySchemeType.ApiKey,
        Name = "x-api-key",
        In = ParameterLocation.Header,
        Scheme = "ApiKeyScheme"
    });
    var scheme = new OpenApiSecurityScheme
    {
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "ApiKey"
        },
        In = ParameterLocation.Header
    };
    var requirement = new OpenApiSecurityRequirement { { scheme, [] } };
    s.AddSecurityRequirement(requirement);
});
builder.Services.AddHttpClient();

var executorType = builder.Configuration.GetSection("Executor").GetValue<string>("Type");
if (executorType == "WebApp")
{
    builder.Services.AddSchedulerServices<WebAppExecutionJob>();
}
else if (executorType == "SelfHosted")
{
    builder.Services.AddExecutorServices(builder.Configuration.GetSection("Executor").GetSection("SelfHosted"));
    builder.Services.AddSchedulerServices<SelfHostedExecutionJob>();
}

// Register Azure Key Vault provider for Always Encrypted.
Biflow.DataAccess.Extensions.RegisterAzureKeyVaultColumnEncryptionKeyStoreProvider(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapDefaultEndpoints(); // Skip authentication for health checks in development.
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.MapGroup("")
        .AddEndpointFilter<ServiceApiKeyEndpointFilter>()
        .MapDefaultEndpoints();
}

if (executorType == "SelfHosted")
{
    app.MapExecutorEndpoints();
}

app.MapSchedulerEndpoints();

app.MapPost("/health/clear", (IEnumerable<HealthService> healthServices) =>
    {
        foreach (var service in healthServices)
        {
            service.ClearErrors();
        }
        return Results.Ok();
    })
    .WithName("ClearHealth")
    .AddEndpointFilter<ServiceApiKeyEndpointFilter>();

app.Run();