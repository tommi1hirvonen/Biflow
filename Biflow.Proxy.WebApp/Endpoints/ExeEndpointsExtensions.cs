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
                TasksRunner<ExeProxyRunResult> runner,
                LinkGenerator linker,
                HttpContext context) => 
            {
                var taskDelegate = ProxyTask.Create(request);
                var id = runner.Run(taskDelegate);
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
                TasksRunner<ExeProxyRunResult> runner,
                LinkGenerator linker,
                HttpContext context) => 
            {
                var status = runner.GetStatus(id);
                var result = status.Match(
                    (Result<ExeProxyRunResult> result) => Results.Ok(new ExeTaskSucceededStatusResponse
                    {
                        Result = result.Value
                    } as ExeTaskStatusResponse),
                    (Error<Exception> error) => Results.Ok(new ExeTaskFailedStatusResponse
                    {
                        ErrorMessage = error.Value.ToString()
                    } as ExeTaskStatusResponse),
                    (Running running) => Results.Accepted(
                        linker.GetUriByName(context, "GetExeStatus", new RouteValueDictionary { { "id", id } }),
                        new ExeTaskRunningStatusResponse() as ExeTaskStatusResponse),
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
                TasksRunner<ExeProxyRunResult> runner,
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