using Biflow.Core;
using Biflow.DataAccess;
using Biflow.Executor.Core;
using Biflow.ExecutorProxy.Core.Authentication;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()
    .AddDatabaseHealthCheck();

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
    s.SwaggerDoc("v1", new OpenApiInfo { Title = "Biflow Executor API", Version = "v1" });
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
builder.Services.AddExecutorServices(builder.Configuration);

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

app.MapExecutorEndpoints();

app.MapPost("/health/clear", (
        [FromKeyedServices(ExecutorServiceKeys.NotificationHealthService)]
        HealthService notificationHealthService,
        [FromKeyedServices(ExecutorServiceKeys.JobExecutorHealthService)]
        HealthService jobExecutorHealthService) =>
    {
        notificationHealthService.ClearErrors();
        jobExecutorHealthService.ClearErrors();
        return Results.Ok();
    })
    .WithName("ClearHealth")
    .WithSummary("Clear transient health errors")
    .WithDescription("Clear transient health errors, such as failed notification attempts.")
    .AddEndpointFilter<ServiceApiKeyEndpointFilter>();

app.Run();