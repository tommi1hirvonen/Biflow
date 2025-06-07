using System.Reflection;

namespace Biflow.Ui.Api;

internal static class EndpointExtensions
{
    public static void MapEndpoints(this WebApplication app, Assembly assembly)
    {
        var endpointTypes = assembly.DefinedTypes
            .Where(type => type.IsClass && type.IsAssignableTo(typeof(IEndpoints)))
            .ToArray();
        foreach (var endpointType in endpointTypes)
        {
            var target = endpointType.GetMethod(nameof(IEndpoints.MapEndpoints),
                BindingFlags.Static | BindingFlags.Public);
            target?.Invoke(null, [app]);
        }
    }
}