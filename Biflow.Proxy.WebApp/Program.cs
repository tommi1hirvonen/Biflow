using System.Diagnostics;
using System.Text;
using Biflow.Executor.Core.Authentication;
using Biflow.Proxy.Core;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

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

var app = builder.Build();

var runGroup = app.MapGroup("/run")
    .AddEndpointFilter<ServiceApiKeyEndpointFilter>();

runGroup.MapPost("/exe", async (RunProxyExeRequest request, CancellationToken cancellationToken) =>
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = request.ExePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            Arguments = string.IsNullOrWhiteSpace(request.Arguments) ? "" : request.Arguments,
            WorkingDirectory = string.IsNullOrEmpty(request.WorkingDirectory) ? "" : request.WorkingDirectory
        };
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        using var process = new Process();
        process.StartInfo = startInfo;
        process.OutputDataReceived += (_, e) => outputBuilder.AppendLine(e.Data);
        process.ErrorDataReceived += (_, e) => errorBuilder.AppendLine(e.Data);
        
        process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        RunProxyExeResponse response;
        string? internalError = null;
        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException e)
        {
            try
            {
                process.Kill(entireProcessTree: true);
                internalError = e.ToString();
            }
            catch (Exception ex)
            {
                internalError = ex.ToString();
            }
        }
        catch (Exception e)
        {
            internalError = e.ToString();
        }
        finally
        {
            var (output, outputTruncated) = outputBuilder.ToString() switch
            {
                { Length: > 500_000 } s1 => (s1[..Math.Min(500_000, s1.Length)], true),
                { Length: > 0 } s2 => (s2, false),
                _ => (null, false)
            };
            var (error, errorTruncated) = errorBuilder.ToString() switch
            {
                { Length: > 500_000 } s1 => (s1[..Math.Min(500_000, s1.Length)], true),
                { Length: > 0 } s2 => (s2, false),
                _ => (null, false)
            };
            response = new RunProxyExeResponse
            {
                ExitCode = process.ExitCode,
                Output = output,
                OutputIsTruncated = outputTruncated,
                ErrorOutput = error,
                ErrorOutputIsTruncated = errorTruncated,
                InternalError = internalError
            };
        }
        
        return response;
    })
    .Produces<RunProxyExeResponse>()
    .WithDescription("Run an executable")
    .WithName("RunExe");

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();