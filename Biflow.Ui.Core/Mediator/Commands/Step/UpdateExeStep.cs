namespace Biflow.Ui.Core;

public class UpdateExeStepCommand : UpdateStepCommand<ExeStep>
{
    public required double TimeoutMinutes { get; init; }
    public required string FilePath { get; init; }
    public required string? Arguments { get; init; }
    public required string? WorkingDirectory { get; init; }
    public required int? SuccessExitCode { get; init; }
    public required Guid? RunAsCredentialId { get; init; }
    public required Guid? ProxyId { get; init; }
    public required UpdateStepParameter[] Parameters { get; init; }
}

[UsedImplicitly]
internal class UpdateExeStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : UpdateStepCommandHandler<UpdateExeStepCommand, ExeStep>(dbContextFactory, validator)
{
    protected override Task<ExeStep?> GetStepAsync(
        Guid stepId, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        return dbContext.ExeSteps
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
    
    protected override async Task UpdateTypeSpecificPropertiesAsync(ExeStep step, UpdateExeStepCommand request,
        AppDbContext dbContext, CancellationToken cancellationToken)
    {
        // Check that the credential exists.
        if (request.RunAsCredentialId is { } id &&
            !await dbContext.Credentials.AnyAsync(x => x.CredentialId == id, cancellationToken))
        {
            throw new NotFoundException<Credential>(id);
        }
        
        step.TimeoutMinutes = request.TimeoutMinutes;
        step.ExeFileName = request.FilePath;
        step.ExeArguments = request.Arguments;
        step.ExeWorkingDirectory = request.WorkingDirectory;
        step.ExeSuccessExitCode = request.SuccessExitCode;
        step.RunAsCredentialId = request.RunAsCredentialId;
        step.ProxyId = request.ProxyId;
        
        await SynchronizeParametersAsync<ExeStepParameter, UpdateStepParameter>(
            step,
            request.Parameters,
            parameter => new ExeStepParameter
            {
                ParameterName = parameter.ParameterName,
                ParameterValue = parameter.ParameterValue,
                UseExpression = parameter.UseExpression,
                Expression = new EvaluationExpression { Expression = parameter.Expression },
                InheritFromJobParameterId = parameter.InheritFromJobParameterId
            },
            dbContext,
            cancellationToken);
    }
}