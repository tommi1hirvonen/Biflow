namespace Biflow.Ui.Api.Endpoints;

[UsedImplicitly]
public abstract class StepsCreateEndpoints : IEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        var apiKeyEndpointFilterFactory = app.Services.GetRequiredService<ApiKeyEndpointFilterFactory>();
        var endpointFilter = apiKeyEndpointFilterFactory.Create([Scopes.JobsWrite]);
        
        var group = app.MapGroup("/jobs")
            .WithTags(Scopes.JobsWrite)
            .AddEndpointFilter(endpointFilter);
        
        group.MapPost("/{jobId:guid}/steps/sql", async (Guid jobId, SqlStepDto stepDto,
            LinkGenerator linker, HttpContext ctx,
            IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new CreateExecutionConditionParameter(
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var parameters = stepDto.Parameters
                    .Select(p => new CreateStepParameter(
                        p.ParameterName,
                        p.ParameterValue,
                        p.UseExpression,
                        p.Expression,
                        p.InheritFromJobParameterId,
                        p.ExpressionParameters
                            .Select(e => new CreateExpressionParameter(e.ParameterName, e.InheritFromJobParameterId))
                            .ToArray()))
                    .ToArray();
                var command = new CreateSqlStepCommand
                {
                    JobId = jobId,
                    StepName = stepDto.StepName,
                    StepDescription = stepDto.StepDescription,
                    ExecutionPhase = stepDto.ExecutionPhase,
                    DuplicateExecutionBehaviour = stepDto.DuplicateExecutionBehaviour,
                    IsEnabled = stepDto.IsEnabled,
                    RetryAttempts = stepDto.RetryAttempts,
                    RetryIntervalMinutes = stepDto.RetryIntervalMinutes,
                    ExecutionConditionExpression = stepDto.ExecutionConditionExpression,
                    StepTagIds = stepDto.StepTagIds,
                    TimeoutMinutes = stepDto.TimeoutMinutes,
                    SqlStatement = stepDto.SqlStatement,
                    ConnectionId = stepDto.ConnectionId,
                    ResultCaptureJobParameterId = stepDto.ResultCaptureJobParameterId,
                    Parameters = parameters,
                    Dependencies = dependencies,
                    ExecutionConditionParameters = executionConditionParameters
                };
                var step = await mediator.SendAsync(command, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetStep", new { stepId = step.StepId });
                return Results.Created(url, step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<SqlStep>(StatusCodes.Status201Created)
            .WithDescription("Create a new SQL step")
            .WithName("CreateSqlStep");
        
        group.MapPost("/{jobId:guid}/steps/package", async (Guid jobId, PackageStepDto stepDto,
                LinkGenerator linker, HttpContext ctx,
                IMediator mediator, CancellationToken cancellationToken) =>
            {
                var dependencies = stepDto.Dependencies.ToDictionary(
                    key => key.DependentOnStepId,
                    value => value.DependencyType);
                var executionConditionParameters = stepDto.ExecutionConditionParameters
                    .Select(p => new CreateExecutionConditionParameter(
                        p.ParameterName,
                        p.ParameterValue,
                        p.InheritFromJobParameterId))
                    .ToArray();
                var parameters = stepDto.Parameters
                    .Select(p => new CreatePackageStepParameter(
                        p.ParameterName,
                        p.ParameterLevel,
                        p.ParameterValue,
                        p.UseExpression,
                        p.Expression,
                        p.InheritFromJobParameterId,
                        p.ExpressionParameters
                            .Select(e => new CreateExpressionParameter(e.ParameterName, e.InheritFromJobParameterId))
                            .ToArray()))
                    .ToArray();
                var command = new CreatePackageStepCommand
                {
                    JobId = jobId,
                    StepName = stepDto.StepName,
                    StepDescription = stepDto.StepDescription,
                    ExecutionPhase = stepDto.ExecutionPhase,
                    DuplicateExecutionBehaviour = stepDto.DuplicateExecutionBehaviour,
                    IsEnabled = stepDto.IsEnabled,
                    RetryAttempts = stepDto.RetryAttempts,
                    RetryIntervalMinutes = stepDto.RetryIntervalMinutes,
                    ExecutionConditionExpression = stepDto.ExecutionConditionExpression,
                    StepTagIds = stepDto.StepTagIds,
                    TimeoutMinutes = stepDto.TimeoutMinutes,
                    ConnectionId = stepDto.ConnectionId,
                    PackageFolderName = stepDto.PackageFolderName,
                    PackageProjectName = stepDto.PackageProjectName,
                    PackageName = stepDto.PackageName,
                    ExecuteIn32BitMode = stepDto.ExecuteIn32BitMode,
                    ExecuteAsLogin = stepDto.ExecuteAsLogin,
                    Parameters = parameters,
                    Dependencies = dependencies,
                    ExecutionConditionParameters = executionConditionParameters
                };
                var step = await mediator.SendAsync(command, cancellationToken);
                var url = linker.GetUriByName(ctx, "GetStep", new { stepId = step.StepId });
                return Results.Created(url, step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<PackageStep>(StatusCodes.Status201Created)
            .WithDescription("Create a new SSIS package step")
            .WithName("CreatePackageStep");
    }
}