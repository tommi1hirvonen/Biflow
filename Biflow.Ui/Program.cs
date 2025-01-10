using Biflow.Ui;
using Biflow.Ui.TableEditor;
using Dapper;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Identity.Web;
using Serilog;
using System.Globalization;
using Biflow.Ui.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddWindowsService();
builder.Services.AddSystemd();

if (builder.Configuration.GetSection("Serilog").Exists())
{
    var logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
    builder.Logging.AddSerilog(logger, dispose: true);
}

builder.Services.AddApplicationInsightsTelemetry();

// Adds all necessary core Biflow UI services.
builder.Services.AddUiCoreServices<UserService>(builder.Configuration);
builder.Services.AddUiAuthentication(builder.Configuration);
builder.Services.AddValidationServices();

builder.Services.AddLocalization();

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

// Use a hosted background service to make sure admin user exists at app startup. 
builder.Services.AddSingleton<EnsureAdminUser>();
builder.Services.AddHostedService(services => services.GetRequiredService<EnsureAdminUser>());

// Add type handlers required by the table editor.
SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
SqlMapper.AddTypeHandler(new TimeOnlyTypeHandler());

// Register Azure Key Vault provider for Always Encrypted.
Biflow.DataAccess.Extensions.RegisterAzureKeyVaultColumnEncryptionKeyStoreProvider(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
}

var supportedCultures = CultureInfo
    .GetCultures(CultureTypes.AllCultures)
    .Select(c => c.Name)
    .ToArray();
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("en-US")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

app.MapStaticAssets();
app.UseAntiforgery();
app.UseCookiePolicy();
app.MapControllers(); // Needed for MicrosoftIdentityUI
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();