using Biflow.Core.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Biflow.Core;

public static class ServiceExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddSingleton<FabricWorkspaceClientFactory>();
        services.AddSingleton<DatasetClientFactory>();
        services.AddSingleton<DataflowClientFactory>();
        return services;
    }
}