using Microsoft.Extensions.DependencyInjection;

namespace Biflow.Ui.Core;

public static class MediatorExtensions
{
    public static IServiceCollection AddRequestHandlers<TScanEntryPoint>(this IServiceCollection services)
    {
        services.Scan(selector =>
        {
            selector.FromAssemblyOf<TScanEntryPoint>()
                .AddClasses(filter => filter.AssignableTo(typeof(IRequestHandler<>)))
                .AsImplementedInterfaces()
                .WithTransientLifetime();
            selector.FromAssemblyOf<TScanEntryPoint>()
                .AddClasses(filter => filter.AssignableTo(typeof(IRequestHandler<,>)))
                .AsImplementedInterfaces()
                .WithTransientLifetime();
        });
        return services;
    }
}