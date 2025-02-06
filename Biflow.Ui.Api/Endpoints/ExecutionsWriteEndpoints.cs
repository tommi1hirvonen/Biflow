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
                builder.AddAll();
                var execution = await builder.SaveExecutionAsync(cancellationToken);
                if (execution is null)
                {
                    return Results.Problem("Execution contained no steps", statusCode: StatusCodes.Status400BadRequest);
                }
                await executor.StartExecutionAsync(execution.ExecutionId, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetExecution",
                    new { executionId = execution.ExecutionId });
                return Results.Created(url, execution);
            })
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<Execution>()
            .WithSummary("Create and start execution")
            .WithDescription("Create and start a new job execution. " +
                             "Optionally provide a list of step ids in the request body JSON model " +
                             "to only include particular steps in the job execution.")
            .WithName("CreateExecution");
    }
}