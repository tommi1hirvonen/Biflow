using System.Diagnostics.CodeAnalysis;
using Biflow.Proxy.Core;
using Biflow.Proxy.WebApp.ProxyTasks;
using OneOf.Types;

namespace Biflow.Proxy.WebApp.Endpoints;

public static class ExeEndpointsExtensions
{
    [SuppressMessage("ReSharper", "RedundantLambdaParameterType")]
    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    public static void MapExeEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapPost("/exe",
            (ExeProxyRunRequest request,
                TasksRunner<ExeProxyTask, ExeTaskRunningStatusResponse, ExeProxyRunResult> runner,
                LinkGenerator linker,
                HttpContext context) => 
            {
                var proxyTask = new ExeProxyTask(request);
                var id = runner.Run(proxyTask);
                var response = new TaskStartedResponse(id);
                var url = linker.GetUriByName(context, "GetExeStatus", new RouteValueDictionary { { "id", id } });
                return Results.Created(url, response);
            })
            .Produces<TaskStartedResponse>(StatusCodes.Status201Created)
            .WithSummary("Run an executable")
            .WithDescription("Run an executable")
            .WithName("RunExe");
        
        builder.MapGet("/exe/{id:guid}", 
            (Guid id,
                TasksRunner<ExeProxyTask, ExeTaskRunningStatusResponse, ExeProxyRunResult> runner,
                LinkGenerator linker,
                HttpContext context) => 
            {
                var status = runner.GetStatus(id);
                var result = status.Match(
                    (Result<ExeProxyRunResult> result) =>
                    {
                        ExeTaskStatusResponse response = new ExeTaskSucceededStatusResponse
                        {
                            Result = result.Value
                        };
                        return Results.Ok(response);
                    },
                    (Error<Exception> error) =>
                    {
                        ExeTaskStatusResponse response = new ExeTaskFailedStatusResponse
                        {
                            ErrorMessage = error.Value.ToString()
                        };
                        return Results.Ok(response);
                    },
                    (Running<ExeTaskRunningStatusResponse> running) =>
                    {
                        ExeTaskStatusResponse response = new ExeTaskRunningStatusResponse
                        {
                            ProcessId = running.Value.ProcessId
                        };
                        var uri = linker.GetUriByName(context, "GetExeStatus",
                            new RouteValueDictionary { { "id", id } });
                        return Results.Accepted(uri, response);
                    },
                    (NotFound notfound) => throw new TaskNotFoundException(id));
                return result;
            })
            .Produces<ExeTaskStatusResponse>()
            .Produces<ExeTaskRunningStatusResponse>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get exe task status")
            .WithDescription("Get the status of an executable task")
            .WithName("GetExeStatus");

        builder.MapPost("/exe/{id:guid}/cancel",
            (Guid id,
                TasksRunner<ExeProxyTask, ExeTaskRunningStatusResponse, ExeProxyRunResult> runner,
                LinkGenerator linker,
                HttpContext context) =>
            {
                if (!runner.Cancel(id))
                    throw new TaskNotFoundException(id);

                var routeValues = new RouteValueDictionary { { "id", id } };
                var url = linker.GetUriByName(context, "GetExeStatus", routeValues);
                return Results.Accepted(url);
            })
            .Produces(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithDescription("Cancel a running executable task")
            .WithSummary("Cancel an executable task")
            .WithName("CancelExe");
    }
}