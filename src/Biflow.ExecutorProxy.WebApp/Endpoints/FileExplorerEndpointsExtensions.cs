using Biflow.ExecutorProxy.Core.FilesExplorer;

namespace Biflow.ExecutorProxy.WebApp.Endpoints;

internal static class FileExplorerEndpointsExtensions
{
    public static void MapFileExplorerEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapPost("/fileexplorer/search", (FileExplorerSearchRequest request) =>
            {
                var items = FileExplorer.GetDirectoryItems(request.Path);
                var response = new FileExplorerSearchResponse(items);
                return Results.Ok(response);
            })
            .WithSummary("Search for local files on the proxy service machine")
            .WithDescription("Search for local files on the proxy service machine")
            .WithName("FileExplorerSearch");
    }
}