var builder = DistributedApplication.CreateBuilder(args);

var proxyApi = builder.AddProject<Projects.Biflow_ExecutorProxy_WebApp>("proxyapi");
proxyApi.WithUrl($"{proxyApi.GetEndpoint("http")}/swagger", "Swagger");

var executorApi = builder.AddProject<Projects.Biflow_Executor_WebApp>("executorapi");
executorApi.WithUrl($"{executorApi.GetEndpoint("http")}/swagger", "Swagger");

var schedulerApi = builder.AddProject<Projects.Biflow_Scheduler_WebApp>("schedulerapi")
    .WithReference(executorApi);
schedulerApi.WithUrl($"{schedulerApi.GetEndpoint("http")}/swagger", "Swagger");

builder.AddProject<Projects.Biflow_Ui>("frontend")
    .WithReference(schedulerApi)
    .WithReference(executorApi)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.Biflow_Ui_Api>("api")
    .WithReference(schedulerApi)
    .WithReference(executorApi)
    .WithExternalHttpEndpoints();

builder.Build().Run();
