var builder = DistributedApplication.CreateBuilder(args);

var executorApi = builder.AddProject<Projects.Biflow_Executor_WebApp>("executorapi")
    .WithExternalHttpEndpoints();

var schedulerApi = builder.AddProject<Projects.Biflow_Scheduler_WebApp>("schedulerapi")
    .WithReference(executorApi);

builder.AddProject<Projects.Biflow_Ui>("frontend")
    .WithReference(schedulerApi)
    .WithReference(executorApi)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.Biflow_Ui_Api>("api")
    .WithReference(schedulerApi)
    .WithReference(executorApi)
    .WithExternalHttpEndpoints();

builder.Build().Run();
