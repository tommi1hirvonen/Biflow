namespace Biflow.Ui.Api.Mediator.Commands;

public record CreateFunctionAppCommand(
    string FunctionAppName,
    string SubscriptionId,
    string ResourceGroupName,
    string ResourceName,
    Guid AzureCredentialId,
    int MaxConcurrentFunctionSteps,
    string? FunctionAppKey) : IRequest<FunctionApp>;

[UsedImplicitly]
internal class CreateFunctionAppCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateFunctionAppCommand, FunctionApp>
{
    public async Task<FunctionApp> Handle(CreateFunctionAppCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        if (!await dbContext.AzureCredentials
                .AnyAsync(x => x.AzureCredentialId == request.AzureCredentialId, cancellationToken))
        {
            throw new NotFoundException<AzureCredential>(request.AzureCredentialId);
        }
        
        var functionApp = new FunctionApp
        {
            FunctionAppName = request.FunctionAppName,
            SubscriptionId = request.SubscriptionId,
            ResourceGroupName = request.ResourceGroupName,
            ResourceName = request.ResourceName,
            AzureCredentialId = request.AzureCredentialId,
            MaxConcurrentFunctionSteps = request.MaxConcurrentFunctionSteps,
            FunctionAppKey = request.FunctionAppKey
        };
        
        functionApp.EnsureDataAnnotationsValidated();
        
        dbContext.FunctionApps.Add(functionApp);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return functionApp;
    }
}