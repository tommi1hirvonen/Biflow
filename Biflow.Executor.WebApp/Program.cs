using Biflow.Executor.Core;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWindowsService();

if (builder.Configuration.GetSection("Serilog").Exists())
{
    var logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
    builder.Logging.AddSerilog(logger, dispose: true);
}

builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();
var windowsAuth = builder.Configuration.GetSection("Authorization").GetSection("Windows");
if (windowsAuth.Exists())
{
    builder.Services.AddAuthorization(options =>
    {
        // If a list of Windows users were defined, require authentication for all endpoints.
        var allowedUsers = windowsAuth.GetSection("AllowedUsers").Get<string[]>();
        if (allowedUsers is null)
        {
            throw new ArgumentNullException(nameof(allowedUsers),
                "Property AllowedUsers must be defined if Windows Authorization is enabled");
        }
        options.FallbackPolicy = new AuthorizationPolicyBuilder().AddRequirements(new UserNamesRequirement(allowedUsers)).Build();
    });
    builder.Services.AddSingleton<IAuthorizationHandler, UserNamesHandler>();
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddExecutorServices(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapExecutorEndpoints();

app.Run();