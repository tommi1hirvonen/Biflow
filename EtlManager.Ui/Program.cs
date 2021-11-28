using EtlManager.DataAccess;
using EtlManager.Ui;
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
builder.Services.AddSingleton<DbHelperService>();

var schedulerType = builder.Configuration.GetSection("Scheduler").GetValue<string>("SchedulerType");
if (schedulerType == "OnPrem")
{
    builder.Services.AddSingleton<ISchedulerService, OnPremSchedulerService>();
}
else if (schedulerType == "Azure")
{
    builder.Services.AddSingleton<ISchedulerService, AzureSchedulerService>();
}
else
{
    throw new ArgumentException($"Error registering scheduler service. Incorrect scheduler type: {schedulerType}. Check appsettings.json.");
}

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