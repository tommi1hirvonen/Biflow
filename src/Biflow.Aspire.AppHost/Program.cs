var builder = DistributedApplication.CreateBuilder(args);

var proxyApi = builder
    .AddProject<Projects.Biflow_ExecutorProxy_WebApp>("proxyapi", options => options.LaunchProfileName = "noBrowser")
    .WithHttpHealthCheck("/health");
proxyApi.WithUrl($"{proxyApi.GetEndpoint("http")}/swagger", "Swagger");

var appDbContext = builder.AddConnectionString("AppDbContext");

var executorApi = builder
    .AddProject<Projects.Biflow_Executor_WebApp>("executorapi", options => options.LaunchProfileName = "noBrowser")
    .WithHttpHealthCheck("/health")
    .WithReference(appDbContext);
executorApi.WithUrl($"{executorApi.GetEndpoint("http")}/swagger", "Swagger");

var schedulerApi = builder
    .AddProject<Projects.Biflow_Scheduler_WebApp>("schedulerapi", options => options.LaunchProfileName = "noBrowser")
    .WithHttpHealthCheck("/health")
    .WithReference(appDbContext)
    .WithReference(executorApi);
schedulerApi.WithUrl($"{schedulerApi.GetEndpoint("http")}/swagger", "Swagger");

builder
    .AddProject<Projects.Biflow_Ui>("frontend")
    .WithHttpHealthCheck("/health")
    .WithReference(appDbContext)
    .WithReference(schedulerApi)
    .WithReference(executorApi)
    .WithExternalHttpEndpoints();

builder
    .AddProject<Projects.Biflow_Ui_Api>("api", options => options.LaunchProfileName = "noBrowser")
    .WithHttpHealthCheck("/health")
    .WithReference(appDbContext)
    .WithReference(schedulerApi)
    .WithReference(executorApi)
    .WithExternalHttpEndpoints();

builder.Build().Run();
