using Biflow.Ui.Mediator.Commands.User;

namespace Biflow.Ui.Services;

internal class EnsureAdminUser(
    ILogger<EnsureAdminUser> logger, IConfiguration configuration, IServiceProvider services) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var adminSection = configuration.GetSection("AdminUser");
            if (adminSection.Exists())
            {
                var adminUsername = adminSection.GetValue<string>("Username");
                ArgumentNullException.ThrowIfNull(adminUsername);
                using var scope = services.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var authentication = configuration.GetValue<string>("Authentication");
                string? adminPassword = null;
                if (authentication == "BuiltIn")
                {
                    adminPassword = adminSection.GetValue<string?>("Password");
                    ArgumentNullException.ThrowIfNull(adminPassword);
                }
                await mediator.SendAsync(new EnsureAdminUserCommand(adminUsername, adminPassword), stoppingToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error ensuring admin user configured in app settings exists in app database");
        }
    }
}