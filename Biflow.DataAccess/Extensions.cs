using Microsoft.Extensions.DependencyInjection;

namespace Biflow.DataAccess;

public static class Extensions
{
    public static IServiceCollection AddExecutionBuilderFactory(this IServiceCollection services)
    {
        services.AddSingleton<IExecutionBuilderFactory, ExecutionBuilderFactory>();
        return services;
    }

    public static IServiceCollection AddDuplicatorServices(this IServiceCollection services)
    {
        services.AddSingleton<StepDuplicatorFactory>();
        return services;
    }

    internal static bool EqualsIgnoreCase(this string text, string? compareTo) =>
        string.Equals(text, compareTo, StringComparison.OrdinalIgnoreCase);
}
