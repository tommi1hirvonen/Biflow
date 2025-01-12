using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Biflow.Core.Converters;
using Biflow.Ui.Api;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddWindowsService();
builder.Services.AddSystemd();

if (builder.Configuration.GetSection("Serilog").Exists())
{
    var logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
    builder.Logging.AddSerilog(logger, dispose: true);
}

builder.Services.AddApplicationInsightsTelemetry();

// Configure response content serialization
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver
    {
        Modifiers = { JsonModifiers.SensitiveModifier }
    };
});
// Configure Swagger model documentation behaviour
builder.Services.Configure<JsonOptions>(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver
    {
        Modifiers = { JsonModifiers.SensitiveModifier }
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(s =>
{
    s.UseOneOfForPolymorphism();
    s.SwaggerDoc("v1", new OpenApiInfo { Title = "Biflow API", Version = "v1" });
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
builder.Services.AddMemoryCache();
builder.Services.AddUiCoreServices<UserService>(builder.Configuration, authenticationConfiguration: "UserAuthentication");
builder.Services.AddSingleton<ApiKeyEndpointFilterFactory>();
builder.Services.AddRequestHandlers<Program>();
builder.Services.AddSingleton<ConcurrentDictionary<Guid, VersionRevertStatus>>();
builder.Services.AddSingleton<VersionRevertService>();
builder.Services.AddHostedService(services => services.GetRequiredService<VersionRevertService>());

var app = builder.Build();

app.UseExceptionHandler(errorAppBuilder =>
{
    errorAppBuilder.Run(async httpContext =>
    {
        if (httpContext.Features.Get<IExceptionHandlerFeature>() is not { Error: { } exception })
        {
            return;
        }
        // Handle known exceptions
        switch (exception)
        {
            case PrimaryKeyException pkException:
                var pkDetails = new ProblemDetails
                {
                    Title = "Primary key conflict",
                    Detail = pkException.Message,
                    Status = StatusCodes.Status409Conflict
                };
                await Results.Problem(pkDetails).ExecuteAsync(httpContext);
                return;
            case NotFoundException notFoundException:
                var notFoundDetails = new ProblemDetails
                {
                    Detail = notFoundException.Message,
                    Status = StatusCodes.Status404NotFound
                };
                await Results.Problem(notFoundDetails).ExecuteAsync(httpContext);
                return;
            case ValidationException validationException:
                var validationErrors = validationException.ValidationResults.ToDictionary();
                await Results.ValidationProblem(validationErrors).ExecuteAsync(httpContext);
                return;
        }
        // In development, return the full exception details.
        if (app.Environment.IsDevelopment())
        {
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            httpContext.Response.ContentType = "text/plain";
            await httpContext.Response.WriteAsync(exception.ToString());
            return;
        }
        await Results.Problem(statusCode: StatusCodes.Status500InternalServerError).ExecuteAsync(httpContext);
    });
});    
app.MapDefaultEndpoints();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.MapEndpoints(Assembly.GetExecutingAssembly());

app.Run();
