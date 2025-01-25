namespace Biflow.Ui.Api.Endpoints;

[UsedImplicitly]
public abstract class StepsUpdateEndpoints : IEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        var apiKeyEndpointFilterFactory = app.Services.GetRequiredService<ApiKeyEndpointFilterFactory>();
        var endpointFilter = apiKeyEndpointFilterFactory.Create([Scopes.JobsWrite]);
        
        var group = app.MapGroup("/jobs")
            .WithTags(Scopes.JobsWrite)
            .AddEndpointFilter(endpointFilter);
        
        group.MapPut("/steps/sql/{stepId:guid}", async (Guid stepId, SqlStepDto stepDto,
            IMediator mediator, CancellationToken cancellationToken) =>
            {
                var parameters = stepDto.Parameters
                    .Select(p => new UpdateStepParameter(
                        p.ParameterId,
                        p.ParameterName,
                        p.ParameterValue,
                        p.UseExpression,
                        p.Expression,
                        p.InheritFromJobParameterId,
                        p.ExpressionParameters
                            .Select(e => new UpdateExpressionParameter(e.ParameterId, e.ParameterName, e.InheritFromJobParameterId))
                            .ToArray()))
                    .ToArray();
                var command = new UpdateSqlStepCommand
                {
                    StepId = stepId,
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
                    Parameters = parameters
                };
                var step = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<SqlStep>()
            .WithDescription("Update an existing SQL step")
            .WithName("UpdateSqlStep");
        
        group.MapPut("/steps/package/{stepId:guid}", async (Guid stepId, PackageStepDto stepDto,
                IMediator mediator, CancellationToken cancellationToken) =>
            {
                var parameters = stepDto.Parameters
                    .Select(p => new UpdatePackageStepParameter(
                        p.ParameterId,
                        p.ParameterName,
                        p.ParameterLevel,
                        p.ParameterValue,
                        p.UseExpression,
                        p.Expression,
                        p.InheritFromJobParameterId,
                        p.ExpressionParameters
                            .Select(e => new UpdateExpressionParameter(e.ParameterId, e.ParameterName, e.InheritFromJobParameterId))
                            .ToArray()))
                    .ToArray();
                var command = new UpdatePackageStepCommand
                {
                    StepId = stepId,
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
                    Parameters = parameters
                };
                var step = await mediator.SendAsync(command, cancellationToken);
                return Results.Ok(step);
            })
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .Produces<PackageStep>()
            .WithDescription("Update an existing SSIS package step")
            .WithName("UpdatePackageStep");
    }
}