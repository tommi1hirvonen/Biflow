using Biflow.Aspire.MigrationService;
using Biflow.DataAccess;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));
builder.Services.AddDbContextFactory<AppDbContext>(lifetime: ServiceLifetime.Scoped);

var host = builder.Build();
host.Run();
