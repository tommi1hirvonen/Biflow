using Biflow.Executor.Core.Exceptions;

namespace Biflow.Ui.Api.Endpoints;

[UsedImplicitly]
public abstract class ExecutionsWriteEndpoints : IEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        var apiKeyEndpointFilterFactory = app.Services.GetRequiredService<ApiKeyEndpointFilterFactory>();
        var endpointFilter = apiKeyEndpointFilterFactory.Create([Scopes.ExecutionsWrite]);
        
        var group = app.MapGroup("/executions")
            .WithTags(Scopes.ExecutionsWrite)
            .AddEndpointFilter(endpointFilter);

        group.MapPost("",
            async (CreateExecution request, IUserService userService,
                IExecutionBuilderFactory<AppDbContext> builderFactory, IExecutorService executor,
                LinkGenerator linker, HttpContext ctx, CancellationToken cancellationToken) =>
            {
                // Create execution builder
                using var builder = await builderFactory.CreateAsync(
                    request.JobId,
                    createdBy: userService.Username,
                    predicates:
                    [
                        _ => step => (request.StepIds == null && step.IsEnabled)
                                     || (request.StepIds != null && request.StepIds.Contains(step.StepId))
                    ],
                    cancellationToken: cancellationToken);
                
                if (builder is null)
                {
                    return Results.Problem($"Could not find job with id {request.JobId}",
                        statusCode: StatusCodes.Status404NotFound);
                }
                
                // Add all steps. Steps have already been filtered in the builder predicates.
                builder.AddAll();
                
                // Apply parameter overrides if any.
                foreach (var parameterOverride in request.JobParameterOverrides)
                {
                    var jobParameter = builder.Parameters
                        .FirstOrDefault(p => p.ParameterId == parameterOverride.ParameterId);
                    if (jobParameter is null)
                    {
                        return Results.Problem($"Could not find job parameter with id {parameterOverride.ParameterId}",
                            statusCode: StatusCodes.Status404NotFound);
                    }
                    jobParameter.ParameterValue = parameterOverride.ParameterValue;
                    jobParameter.UseExpression = parameterOverride.UseExpression;
                    jobParameter.Expression.Expression = parameterOverride.Expression;
                }
                
                // Save the execution to DB.
                var execution = await builder.SaveExecutionAsync(cancellationToken);
                if (execution is null)
                {
                    return Results.Problem("Execution contained no steps", statusCode: StatusCodes.Status400BadRequest);
                }
                
                await executor.StartExecutionAsync(execution.ExecutionId, cancellationToken);
                
                var url = linker.GetUriByName(ctx, "GetExecution",
                    new { executionId = execution.ExecutionId });
                return Results.Accepted(url, execution);
            })
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<Execution>()
            .WithSummary("Create and start execution")
            .WithDescription("Create and start a new job execution. " +
                             "Optionally provide a list of step ids in the request body JSON model " +
                             "to only include particular steps in the job execution.")
            .WithName("CreateExecution");
        
        group.MapPost("/{executionId:guid}/stop",
            async (Guid executionId, IExecutorService executor, IUserService userService,
                LinkGenerator linker, HttpContext ctx) =>
            {
                try
                {
                    await executor.StopExecutionAsync(executionId, userService.Username ?? "API");
                    var url = linker.GetUriByName(ctx, "GetExecution",
                        new { executionId });
                    return Results.Accepted(url);
                }
                catch (ExecutionNotFoundException)
                {
                    return Results.Problem("Execution not found", statusCode: StatusCodes.Status404NotFound);
                }
                catch (HttpRequestException ex) when ((int?)ex.StatusCode == StatusCodes.Status404NotFound)
                {
                    return Results.Problem("Execution not found", statusCode: StatusCodes.Status404NotFound);
                }
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status202Accepted)
            .WithSummary("Stop job execution")
            .WithDescription("Stop a running job execution")
            .WithName("StopExecution");
        
        group.MapPost("/{executionId:guid}/steps/{stepId:guid}/stop",
                async (Guid executionId, Guid stepId, IExecutorService executor, IUserService userService,
                    LinkGenerator linker, HttpContext ctx) =>
                {
                    try
                    {
                        await executor.StopExecutionAsync(executionId, stepId, userService.Username ?? "API");
                        var url = linker.GetUriByName(ctx, "GetExecutionStep",
                            new { executionId, stepId });
                        return Results.Accepted(url);
                    }
                    catch (ExecutionNotFoundException)
                    {
                        return Results.Problem("Execution not found", statusCode: StatusCodes.Status404NotFound);
                    }
                    catch (HttpRequestException ex) when ((int?)ex.StatusCode == StatusCodes.Status404NotFound)
                    {
                        return Results.Problem("Execution not found", statusCode: StatusCodes.Status404NotFound);
                    }
                })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status202Accepted)
            .WithSummary("Stop step execution")
            .WithDescription("Stop a running step execution")
            .WithName("StopExecutionStep");
    }
}