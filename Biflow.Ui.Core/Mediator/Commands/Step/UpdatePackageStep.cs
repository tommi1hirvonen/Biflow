namespace Biflow.Ui.Core;

public class UpdatePackageStepCommand : UpdateStepCommand<PackageStep>
{
    public required double TimeoutMinutes { get; init; }
    public required Guid ConnectionId { get; init; }
    public required string PackageFolderName { get; init; }
    public required string PackageProjectName { get; init; }
    public required string PackageName { get; init; } 
    public required bool ExecuteIn32BitMode { get; init; }
    public required string ExecuteAsLogin { get; init; }
    public required UpdatePackageStepParameter[] Parameters { get; init; }
}

[UsedImplicitly]
internal class UpdatePackageStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : UpdateStepCommandHandler<UpdatePackageStepCommand, PackageStep>(dbContextFactory, validator)
{
    protected override Task<PackageStep?> GetStepAsync(
        Guid stepId, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        return dbContext.PackageSteps
            .Include(step => step.StepParameters)
            .ThenInclude(p => p.InheritFromJobParameter)
            .Include(step => step.StepParameters)
            .ThenInclude(p => p.ExpressionParameters)
            .Include(step => step.Tags)
            .Include(step => step.Dependencies)
            .Include(step => step.DataObjects)
            .ThenInclude(s => s.DataObject)
            .Include(step => step.ExecutionConditionParameters)
            .FirstOrDefaultAsync(step => step.StepId == stepId, cancellationToken);
    }
    
    protected override async Task UpdateTypeSpecificPropertiesAsync(PackageStep step, UpdatePackageStepCommand request,
        AppDbContext dbContext, CancellationToken cancellationToken)
    {
        // Check that the connection exists.
        if (!await dbContext.SqlConnections.AnyAsync(c => c.ConnectionId == request.ConnectionId, cancellationToken))
        {
            throw new NotFoundException<SqlConnectionBase>(request.ConnectionId);
        }
        
        step.TimeoutMinutes = request.TimeoutMinutes;
        step.ConnectionId = request.ConnectionId;
        step.PackageFolderName = request.PackageFolderName;
        step.PackageProjectName = request.PackageProjectName;
        step.PackageName = request.PackageName;
        step.ExecuteIn32BitMode = request.ExecuteIn32BitMode;
        step.ExecuteAsLogin = request.ExecuteAsLogin;
        
        await SynchronizeParametersAsync<PackageStepParameter, UpdatePackageStepParameter>(
            step,
            request.Parameters,
            parameter => new PackageStepParameter
            {
                ParameterName = parameter.ParameterName,
                ParameterLevel = parameter.ParameterLevel,
                ParameterValue = parameter.ParameterValue,
                UseExpression = parameter.UseExpression,
                Expression = new EvaluationExpression { Expression = parameter.Expression },
                InheritFromJobParameterId = parameter.InheritFromJobParameterId
            },
            dbContext,
            cancellationToken);
        
        // Update ParameterLevel for matching parameters as SynchronizeParameters() does not handle that.
        foreach (var parameter in step.StepParameters)
        {
            var updateParameter = request.Parameters
                .FirstOrDefault(p => p.ParameterId == parameter.ParameterId);
            if (updateParameter is null) continue;
            parameter.ParameterLevel = updateParameter.ParameterLevel;
        }
    }
}