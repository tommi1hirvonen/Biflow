using Biflow.DataAccess;
using Biflow.Executor.Core;
using Biflow.Executor.Core.WebExtensions;
using Biflow.Scheduler.Core;
using Biflow.Ui;
using Biflow.Ui.Services;
using Havit.Blazor.Components.Web;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

if (builder.Configuration.GetSection("Serilog").Exists())
{
    var logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
    builder.Logging.AddSerilog(logger, dispose: true);
}

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpContextAccessor();

var connectionString = builder.Configuration.GetConnectionString("BiflowContext");
builder.Services.AddDbContextFactory<BiflowContext>(options =>
{
    options.UseSqlServer(connectionString, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
    //options.EnableSensitiveDataLogging();
});
    

builder.Services.AddHxServices();
builder.Services.AddHxMessenger();
builder.Services.AddHxMessageBoxHost();

builder.Services.AddHttpClient();
builder.Services.AddHttpClient("DefaultCredentials")
    // Passes Windows credentials in on-premise installations to the scheduler API.
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseDefaultCredentials = true });

builder.Services.AddSingleton<ITokenService, TokenService>();

var executorType = builder.Configuration.GetSection("Executor").GetValue<string>("Type");
if (executorType == "ConsoleApp")
{
    builder.Services.AddSingleton<IExecutorService, ConsoleAppExecutorService>();
}
else if (executorType == "WebApp")
{
    builder.Services.AddSingleton<IExecutorService, WebAppExecutorService>();
}
else if (executorType == "SelfHosted")
{
    builder.Services.AddExecutorServices<ExecutorLauncher>(connectionString, builder.Configuration.GetSection("Executor").GetSection("SelfHosted"));
    builder.Services.AddSingleton<ExecutionManager>();
    builder.Services.AddSingleton<IExecutorService, SelfHostedExecutorService>();
}
else
{
    throw new ArgumentException($"Error registering executor service. Incorrect executor type: {executorType}. Check appsettings.json.");
}

var schedulerType = builder.Configuration.GetSection("Scheduler").GetValue<string>("Type");
if (schedulerType == "WebApp")
{
    builder.Services.AddSingleton<ISchedulerService, WebAppSchedulerService>();
}
else if (schedulerType == "SelfHosted")
{
    builder.Services.AddSchedulerServices<ExecutionJob>(connectionString);
    builder.Services.AddSingleton<ISchedulerService, SelfHostedSchedulerService>();
}
else
{
    throw new ArgumentException($"Error registering scheduler service. Incorrect scheduler type: {schedulerType}. Check appsettings.json.");
}

builder.Services.AddSingleton<DbHelperService>();
builder.Services.AddSingleton<SqlServerHelperService>();
builder.Services.AddSingleton<MarkupHelperService>();
builder.Services.AddSingleton<SubscriptionsHelperService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseCookiePolicy();
app.UseAuthentication();
app.UseEndpoints(endpoints =>
{
    endpoints.MapBlazorHub();
    endpoints.MapFallbackToPage("/_Host");
});

if (schedulerType == "SelfHosted")
{
    // Read all schedules into the schedules manager.
    using var scope = app.Services.CreateScope();
    var scheduler = scope.ServiceProvider.GetRequiredService<ISchedulerService>();
    await scheduler.SynchronizeAsync();
}

app.Run();