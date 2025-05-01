using System.Text.Json.Serialization;
using Biflow.Proxy.Core;
using Biflow.Proxy.Core.Authentication;
using Biflow.Proxy.WebApp;
using Biflow.Proxy.WebApp.Endpoints;
using Microsoft.AspNetCore.Routing.Constraints;

var builder = WebApplication.CreateSlimBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddEndpointsApiExplorer()
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