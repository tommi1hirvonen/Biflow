using Biflow.Executor.Core;
using Biflow.Scheduler.Core;
using Biflow.Scheduler.WebApp;
using Biflow.Scheduler.WebApp.Authentication;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWindowsService();

if (builder.Configuration.GetSection("Serilog").Exists())
{
    var logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
    builder.Logging.AddSerilog(logger, dispose: true);
}

builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(s =>
{
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

var useApiKeyAuth = builder.Configuration
    .GetSection(AuthConstants.Authentication)
    .GetValue<string>(AuthConstants.ApiKey) is not null;

if (useApiKeyAuth)
{
    builder.Services.AddAuthorization();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (useApiKeyAuth)
{
    app.UseMiddleware<ApiKeyAuthMiddleware>();
    app.UseAuthorization();
}

if (executorType == "SelfHosted")
{
    app.MapExecutorEndpoints();
}

app.MapSchedulerEnpoints();

app.Run();