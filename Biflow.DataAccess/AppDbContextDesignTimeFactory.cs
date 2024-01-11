using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Biflow.DataAccess;

internal class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var settings = new Dictionary<string, string?>
        {
            {
                "ConnectionStrings:AppDbContext",
                "Data Source=localhost;Database=Biflow;Integrated Security=sspi;Encrypt=true;TrustServerCertificate=true;"
            }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
        return new AppDbContext(configuration, null!, null!);
    }
}
