using Microsoft.Extensions.DependencyInjection;

namespace Biflow.DataAccess;

public static class Extensions
{
    public static IServiceCollection AddExecutionBuilderFactory<TDbContext>(this IServiceCollection services)
        where TDbContext : BiflowContext
    {
        services.AddSingleton(typeof(IExecutionBuilderFactory<TDbContext>), typeof(ExecutionBuilderFactory<TDbContext>));
        return services;
    }

    public static IServiceCollection AddDuplicatorServices(this IServiceCollection services)
    {
        services.AddSingleton<StepsDuplicatorFactory>();
        services.AddSingleton<JobDuplicatorFactory>();
        return services;
    }

    internal static bool EqualsIgnoreCase(this string text, string? compareTo) =>
        string.Equals(text, compareTo, StringComparison.OrdinalIgnoreCase);
}
