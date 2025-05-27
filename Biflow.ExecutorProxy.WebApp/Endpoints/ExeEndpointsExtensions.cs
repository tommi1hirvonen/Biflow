using System.Diagnostics.CodeAnalysis;
using Biflow.ExecutorProxy.Core;
using Biflow.ExecutorProxy.WebApp.ProxyTasks;
using OneOf.Types;

namespace Biflow.ExecutorProxy.WebApp.Endpoints;

internal static class ExeEndpointsExtensions
{
    [SuppressMessage("ReSharper", "RedundantLambdaParameterType")]
    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    public static void MapExeEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapPost("/exe",
            (ExeProxyRunRequest request,
                ExeTasksRunner runner,
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
            .WithDescription("Run an executable asynchronously. " +
                             "The executable will be started immediately and its status can be queried using the returned id. " +
                             "The id can also be used to cancel the executable task.")
            .WithName("RunExe");
        
        builder.MapGet("/exe/{id:guid}", 
            (Guid id,
                ExeTasksRunner runner,
                LinkGenerator linker,
                HttpContext context) => 
            {
                var status = runner.GetStatus(id);
                var result = status.Match(
                    (Result<ExeTaskCompletedResponse> result) =>
                    {
                        ExeTaskStatusResponse response = new ExeTaskCompletedResponse
                        {
                            ProcessId = result.Value.ProcessId,
                            ExitCode = result.Value.ExitCode,
                            Output = result.Value.Output,
                            OutputIsTruncated = result.Value.OutputIsTruncated,
                            ErrorOutput = result.Value.ErrorOutput,
                            ErrorOutputIsTruncated = result.Value.ErrorOutputIsTruncated,
                            InternalError = result.Value.InternalError
                        };
                        return Results.Ok(response);
                    },
                    (Error<Exception> error) =>
                    {
                        ExeTaskStatusResponse response = new ExeTaskFailedResponse
                        {
                            ErrorMessage = error.Value.ToString()
                        };
                        return Results.Ok(response);
                    },
                    (Running<ExeTaskRunningResponse> running) =>
                    {
                        ExeTaskStatusResponse response = new ExeTaskRunningResponse
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
            .Produces<ExeTaskRunningResponse>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get exe task status")
            .WithDescription("Get the status of an executable task")
            .WithName("GetExeStatus");

        builder.MapPost("/exe/{id:guid}/cancel",
            (Guid id,
                ExeTasksRunner runner,
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