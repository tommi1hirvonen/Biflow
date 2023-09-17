using Biflow.Ui.Components;
using Biflow.Ui.Core;
using Havit.Blazor.Components.Web;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Identity.Web;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

if (WindowsServiceHelpers.IsWindowsService())
{
    builder.Host.UseWindowsService();
}

if (builder.Configuration.GetSection("Serilog").Exists())
{
    var logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
    builder.Logging.AddSerilog(logger, dispose: true);
}

// Adds all necessary core Biflow UI services.
builder.Services.AddUiCoreServices(builder.Configuration);
builder.Services.AddUiCoreAuthentication(builder.Configuration);
builder.Services.AddValidationServices();
builder.Services.AddScoped<ThemeService>();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor().AddMicrosoftIdentityConsentHandler();
builder.Services.AddHttpContextAccessor();

builder.Services.AddHxServices();
builder.Services.AddHxMessenger();
builder.Services.AddHxMessageBoxHost();

var schedulerType = builder.Configuration.GetSection("Scheduler").GetValue<string>("Type");

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
app.MapControllers();
app.UseCookiePolicy();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

if (schedulerType == "SelfHosted")
{
    await app.ReadAllSchedulesAsync();
}

app.Run();