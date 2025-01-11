using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Biflow.Core.Converters;
using Biflow.Ui.Api;
using Biflow.Ui.Api.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
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
builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
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

var app = builder.Build();

app.UseExceptionHandler(errorAppBuilder =>
{
    errorAppBuilder.Run(async httpContext =>
    {
        if (httpContext.Features.Get<IExceptionHandlerFeature>() is not { Error: { } exception })
        {
            return;
        }
        switch (exception)
        {
            case PrimaryKeyException pkException:
                httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
                httpContext.Response.ContentType = "text/plain";
                await httpContext.Response.WriteAsync(pkException.Message);
                return;
            case NotFoundException notFoundException:
                httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                httpContext.Response.ContentType = "text/plain";
                await httpContext.Response.WriteAsync(notFoundException.Message);
                return;
        }
        if (app.Environment.IsDevelopment())
        {
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            httpContext.Response.ContentType = "text/plain";
            await httpContext.Response.WriteAsync(exception.ToString());
        }
    });
});    
app.MapDefaultEndpoints();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.MapEndpoints(Assembly.GetExecutingAssembly());

app.Run();
