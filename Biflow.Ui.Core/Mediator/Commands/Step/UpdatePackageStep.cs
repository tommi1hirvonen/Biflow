namespace Biflow.Ui.Core;

public class UpdatePackageStepCommand : UpdateStepCommand<PackageStep>
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
internal class UpdatePackageStepCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    StepValidator validator
) : UpdateStepCommandHandler<UpdatePackageStepCommand, PackageStep>(dbContextFactory, validator)
{
    protected override async Task UpdatePropertiesAsync(
        PackageStep step, UpdatePackageStepCommand request, AppDbContext dbContext, CancellationToken cancellationToken)
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
    }
}