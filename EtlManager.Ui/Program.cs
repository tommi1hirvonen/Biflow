using EtlManager.DataAccess;
using EtlManager.Executor.Core;
using EtlManager.Executor.WebApp;
using EtlManager.Scheduler.Core;
using EtlManager.Ui;
using EtlManager.Ui.Services;
using Havit.Blazor.Components.Web;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpContextAccessor();

var connectionString = builder.Configuration.GetConnectionString("EtlManagerContext");
builder.Services.AddDbContextFactory<EtlManagerContext>(options =>
    options.UseSqlServer(connectionString, o =>
        o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

builder.Services.AddHxServices();
builder.Services.AddHxMessenger();
builder.Services.AddHxMessageBoxHost();

builder.Services.AddHttpClient();
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
if (schedulerType == "WinService")
{
    builder.Services.AddSingleton<ISchedulerService, WinServiceSchedulerService>();
}
else if (schedulerType == "WebApp")
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

app.Run();