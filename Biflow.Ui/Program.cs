using Biflow.Ui;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

builder.Services.AddApplicationInsightsTelemetry();

// Adds all necessary core Biflow UI services.
builder.Services.AddUiCoreServices(builder.Configuration);
builder.Services.AddUiCoreAuthentication(builder.Configuration);
builder.Services.AddValidationServices();

builder.Services.AddControllers(); // Needed for MicrosoftIdentityUI
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddMicrosoftIdentityConsentHandler();

builder.Services.TryAddEnumerable(
    ServiceDescriptor.Scoped<CircuitHandler, UserCircuitHandler>());

builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<ContextMenuService>();
builder.Services.AddScoped<ToasterService>();

builder.Services.AddHxServices();
builder.Services.AddHxMessageBoxHost();

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
app.UseAntiforgery();
app.UseCookiePolicy();
app.MapControllers(); // Needed for MicrosoftIdentityUI
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.EnsureAdminUserAsync();

app.Run();