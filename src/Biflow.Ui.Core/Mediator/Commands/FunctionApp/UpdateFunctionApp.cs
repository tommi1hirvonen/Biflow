namespace Biflow.Ui.Core;

/// <summary>
/// 
/// </summary>
/// <param name="FunctionAppId"></param>
/// <param name="FunctionAppName"></param>
/// <param name="SubscriptionId"></param>
/// <param name="ResourceGroupName"></param>
/// <param name="ResourceName"></param>
/// <param name="AzureCredentialId"></param>
/// <param name="MaxConcurrentFunctionSteps"></param>
/// <param name="FunctionAppKey">Pass null to retain the previous FunctionAppKey value, pass empty string to reset</param>
public record UpdateFunctionAppCommand(
    Guid FunctionAppId,
    string FunctionAppName,
    string SubscriptionId,
    string ResourceGroupName,
    string ResourceName,
    Guid AzureCredentialId,
    int MaxConcurrentFunctionSteps,
    string? FunctionAppKey) : IRequest<FunctionApp>;

[UsedImplicitly]
internal class UpdateFunctionAppCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateFunctionAppCommand, FunctionApp>
{
    public async Task<FunctionApp> Handle(UpdateFunctionAppCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var functionApp = await dbContext.FunctionApps
            .FirstOrDefaultAsync(f => f.FunctionAppId == request.FunctionAppId, cancellationToken)
            ?? throw new NotFoundException<FunctionApp>(request.FunctionAppId);
        
        if (!await dbContext.AzureCredentials
                .AnyAsync(x => x.AzureCredentialId == request.AzureCredentialId, cancellationToken))
        {
            throw new NotFoundException<AzureCredential>(request.AzureCredentialId);
        }
        
        functionApp.FunctionAppName = request.FunctionAppName;
        functionApp.SubscriptionId = request.SubscriptionId;
        functionApp.ResourceGroupName = request.ResourceGroupName;
        functionApp.ResourceName = request.ResourceName;
        functionApp.AzureCredentialId = request.AzureCredentialId;
        functionApp.MaxConcurrentFunctionSteps = request.MaxConcurrentFunctionSteps;
        if (request.FunctionAppKey is { Length: 0 })
        {
            functionApp.FunctionAppKey = null; // Reset FunctionAppKey to null to reset the value in the database.
        }
        else if (request.FunctionAppKey is not null)
        {
            functionApp.FunctionAppKey = request.FunctionAppKey;
        }
        
        functionApp.EnsureDataAnnotationsValidated();
        
        await dbContext.SaveChangesAsync(cancellationToken);
        return functionApp;
    }
}