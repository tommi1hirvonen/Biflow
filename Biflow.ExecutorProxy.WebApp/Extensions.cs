using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

namespace Biflow.ExecutorProxy.WebApp;

internal static class Extensions
{
    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(s =>
        {
            s.UseOneOfForPolymorphism();
            s.SwaggerDoc("v1", new OpenApiInfo { Title = "Biflow Proxy API", Version = "v1" });
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
        
        return services;
    }

    public static void UseExceptionHandler(this WebApplication app)
    {
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
                    case BadHttpRequestException { InnerException: JsonException jsonException }:
                        var badRequestDetails = new ProblemDetails
                        {
                            Title = "Bad Request",
                            Detail = jsonException.Message,
                            Status = StatusCodes.Status400BadRequest
                        };
                        await Results.Problem(badRequestDetails).ExecuteAsync(httpContext);
                        return;
                    case TaskNotFoundException notFoundException:
                        var notFoundDetails = new ProblemDetails
                        {
                            Detail = notFoundException.Message,
                            Status = StatusCodes.Status404NotFound
                        };
                        await Results.Problem(notFoundDetails).ExecuteAsync(httpContext);
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
    }
}