namespace Biflow.Ui.Core;

public class CreatePackageStepCommand : CreateStepCommand<PackageStep>
{
    public required double TimeoutMinutes { get; init; }
    public required Guid ConnectionId { get; init; }
    public required string PackageFolderName { get; init; }
    public required string PackageProjectName { get; init; }
    public required string PackageName { get; init; } 
    public required bool ExecuteIn32BitMode { get; init; }
    public required string? ExecuteAsLogin { get; init; }
    public required CreatePackageStepParameter[] Parameters { get; init; }
}

[UsedImplicitly]
internal class CreatePackageStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : CreateStepCommandHandler<CreatePackageStepCommand, PackageStep>(dbContextFactory, validator)
{
    protected override async Task<PackageStep> CreateStepAsync(
        CreatePackageStepCommand request, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        // Check that the connection exists.
        if (!await dbContext.MsSqlConnections.AnyAsync(c => c.ConnectionId == request.ConnectionId, cancellationToken))
        {
            throw new NotFoundException<MsSqlConnection>(request.ConnectionId);
        }
        
        var step = new PackageStep
        {
            JobId = request.JobId,
            StepName = request.StepName,
            StepDescription = request.StepDescription,
            ExecutionPhase = request.ExecutionPhase,
            DuplicateExecutionBehaviour = request.DuplicateExecutionBehaviour,
            IsEnabled = request.IsEnabled,
            RetryAttempts = request.RetryAttempts,
            RetryIntervalMinutes = request.RetryIntervalMinutes,
            ExecutionConditionExpression = new EvaluationExpression
                { Expression = request.ExecutionConditionExpression },
            TimeoutMinutes = request.TimeoutMinutes,
            ConnectionId = request.ConnectionId,
            PackageFolderName = request.PackageFolderName,
            PackageProjectName = request.PackageProjectName,
            PackageName = request.PackageName,
            ExecuteIn32BitMode = request.ExecuteIn32BitMode,
            ExecuteAsLogin = request.ExecuteAsLogin
        };
        
        foreach (var createParameter in request.Parameters)
        {
            var parameter = new PackageStepParameter
            {
                ParameterName = createParameter.ParameterName,
                ParameterLevel = createParameter.ParameterLevel,
                ParameterValue = createParameter.ParameterValue,
                UseExpression = createParameter.UseExpression,
                Expression = new EvaluationExpression { Expression = createParameter.Expression },
                InheritFromJobParameterId = createParameter.InheritFromJobParameterId
            };
            foreach (var createExpressionParameter in createParameter.ExpressionParameters)
            {
                parameter.AddExpressionParameter(
                    createExpressionParameter.ParameterName,
                    createExpressionParameter.InheritFromJobParameterId);
            }
            step.StepParameters.Add(parameter);
        }

        return step;
    }
}