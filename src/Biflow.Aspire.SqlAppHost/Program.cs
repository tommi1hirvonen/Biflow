var builder = DistributedApplication.CreateBuilder(args);

var proxyApi = builder
    .AddProject<Projects.Biflow_ExecutorProxy_WebApp>("proxyapi", options => options.LaunchProfileName = "noBrowser")
    .WithHttpHealthCheck("/health");
proxyApi.WithUrl($"{proxyApi.GetEndpoint("http")}/swagger", "Swagger");

var sql = builder
    .AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent);

var db = sql.AddDatabase("AppDbContext");

var migrations = builder
    .AddProject<Projects.Biflow_Aspire_MigrationService>("migrations")
    .WithReference(db)
    .WaitFor(db);

var executorApi = builder
    .AddProject<Projects.Biflow_Executor_WebApp>("executorapi", options => options.LaunchProfileName = "noBrowser")
    .WithHttpHealthCheck("/health")
    .WithReference(db)
    .WithReference(migrations)
    .WaitFor(db)
    .WaitForCompletion(migrations);
executorApi.WithUrl($"{executorApi.GetEndpoint("http")}/swagger", "Swagger");

var schedulerApi = builder
    .AddProject<Projects.Biflow_Scheduler_WebApp>("schedulerapi", options => options.LaunchProfileName = "noBrowser")
    .WithHttpHealthCheck("/health")
    .WithReference(db)
    .WithReference(migrations)
    .WithReference(executorApi)
    .WaitFor(db)
    .WaitForCompletion(migrations);
schedulerApi.WithUrl($"{schedulerApi.GetEndpoint("http")}/swagger", "Swagger");

builder
    .AddProject<Projects.Biflow_Ui>("frontend")
    .WithHttpHealthCheck("/health")
    .WithReference(db)
    .WithReference(migrations)
    .WithReference(schedulerApi)
    .WithReference(executorApi)
    .WithExternalHttpEndpoints()
    .WaitFor(db)
    .WaitForCompletion(migrations);

builder
    .AddProject<Projects.Biflow_Ui_Api>("api", options => options.LaunchProfileName = "noBrowser")
    .WithHttpHealthCheck("/health")
    .WithReference(db)
    .WithReference(migrations)
    .WithReference(schedulerApi)
    .WithReference(executorApi)
    .WithExternalHttpEndpoints()
    .WaitFor(db)
    .WaitForCompletion(migrations);

builder.Build().Run();