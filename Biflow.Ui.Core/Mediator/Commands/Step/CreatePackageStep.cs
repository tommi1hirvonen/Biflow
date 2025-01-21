namespace Biflow.Ui.Core;

public class CreatePackageStepCommand : CreateStepCommand<PackageStep>
{
    public required int TimeoutMinutes { get; init; }
    public required Guid ConnectionId { get; init; }
    public required string PackageFolderName { get; init; }
    public required string PackageProjectName { get; init; }
    public required string PackageName { get; init; } 
    public required bool ExecuteIn32BitMode { get; init; }
    public required string ExecuteAsLogin { get; init; } 
}

[UsedImplicitly]
internal class CreatePackageStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : CreateStepCommandHandler<CreatePackageStepCommand, PackageStep>(dbContextFactory, validator)
{
    protected override Guid GetJobId(CreatePackageStepCommand request) => request.JobId;

    protected override Guid[] GetStepTagIds(CreatePackageStepCommand request) => request.StepTagIds;

    protected override PackageStep CreateStep(CreatePackageStepCommand request) => new()
    {
        JobId = request.JobId,
        StepName = request.StepName,
        StepDescription = request.StepDescription,
        ExecutionPhase = request.ExecutionPhase,
        DuplicateExecutionBehaviour = request.DuplicateExecutionBehaviour,
        IsEnabled = request.IsEnabled,
        RetryAttempts = request.RetryAttempts,
        RetryIntervalMinutes = request.RetryIntervalMinutes,
        ExecutionConditionExpression = new EvaluationExpression { Expression = request.ExecutionConditionExpression },
        TimeoutMinutes = request.TimeoutMinutes,
        ConnectionId = request.ConnectionId,
        PackageFolderName = request.PackageFolderName,
        PackageProjectName = request.PackageProjectName,
        PackageName = request.PackageName, 
        ExecuteIn32BitMode = request.ExecuteIn32BitMode,
        ExecuteAsLogin = request.ExecuteAsLogin 
    };
}