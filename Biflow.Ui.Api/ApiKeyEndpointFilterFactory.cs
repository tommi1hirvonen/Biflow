namespace Biflow.Ui.Api;

internal class ApiKeyEndpointFilterFactory(IServiceProvider serviceProvider)
{
    public ApiKeyEndpointFilter Create(IEnumerable<string> requiredScopes)
    {
        IReadOnlyList<string> scopes = requiredScopes.ToArray();
        return ActivatorUtilities.CreateInstance<ApiKeyEndpointFilter>(serviceProvider, scopes);
    }
}