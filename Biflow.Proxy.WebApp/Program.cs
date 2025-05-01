using Biflow.Executor.Core.Authentication;
using Biflow.Proxy.Core;
using Biflow.Proxy.WebApp;
using Biflow.Proxy.WebApp.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddEndpointsApiExplorer()
    .AddSwagger()
    .AddSingleton<TasksRunner<ExeProxyRunResult>>();

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