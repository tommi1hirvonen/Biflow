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
        builder.MapPost("/exe", (ExeProxyRunRequest request, TasksRunner<ExeProxyRunResult> runner) => 
            {
                var taskDelegate = ProxyTask.Create(request);
                var id = runner.Run(taskDelegate);
                return new TaskStartedResponse(id);
            })
            .Produces<TaskStartedResponse>()
            .WithDescription("Run an executable")
            .WithName("RunExe");
        
        builder.MapGet("/exe/{id:guid}", (Guid id, TasksRunner<ExeProxyRunResult> runner) => 
            {
                var status = runner.GetStatus(id);
                var response = status.Match<ExeTaskStatusResponse>(
                    (Result<ExeProxyRunResult> result) => new ExeTaskSucceededStatusResponse
                    {
                        Result = result.Value
                    },
                    (Error<Exception> error) => new ExeTaskFailedStatusResponse
                    {
                        ErrorMessage = error.Value.ToString()
                    },
                    (Running running) => new ExeTaskRunningStatusResponse(),
                    (NotFound notfound) => throw new TaskNotFoundException(id));
                return response;
            })
            .Produces<ExeTaskStatusResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithDescription("Get the status of a running executable")
            .WithName("GetExeStatus");

        builder.MapPost("/exe/{id:guid}/cancel", (Guid id, TasksRunner<ExeProxyRunResult> runner) =>
            {
                if (runner.Cancel(id))
                    return Results.Ok();
        
                throw new TaskNotFoundException(id);
            })
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithDescription("Cancel a running executable")
            .WithName("CancelExe");
    }
}