using Microsoft.Extensions.DependencyInjection;

namespace Biflow.DataAccess;

public static class Extensions
{
    public static IServiceCollection AddExecutionBuilder(this IServiceCollection services)
    {
        services.AddSingleton<IExecutionBuilderFactory, ExecutionBuilderFactory>();
        return services;
    }

    internal static bool EqualsIgnoreCase(this string text, string? compareTo) =>
        string.Equals(text, compareTo, StringComparison.OrdinalIgnoreCase);
}
