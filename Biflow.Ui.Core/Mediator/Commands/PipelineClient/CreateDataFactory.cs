namespace Biflow.Ui.Core;

public record CreateDataFactoryCommand(
    string PipelineClientName,
    int MaxConcurrentPipelineSteps,
    Guid AzureCredentialId,
    string SubscriptionId,
    string ResourceGroupName,
    string ResourceName) : IRequest<DataFactory>;

[UsedImplicitly]
internal class CreateDataFactoryCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateDataFactoryCommand, DataFactory>
{
    public async Task<DataFactory> Handle(CreateDataFactoryCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        if (!await dbContext.AzureCredentials
                .AnyAsync(x => x.AzureCredentialId == request.AzureCredentialId, cancellationToken))
        {
            throw new NotFoundException<AzureCredential>(request.AzureCredentialId);
        }

        var dataFactory = new DataFactory
        {
            PipelineClientName = request.PipelineClientName,
            MaxConcurrentPipelineSteps = request.MaxConcurrentPipelineSteps,
            AzureCredentialId = request.AzureCredentialId,
            SubscriptionId = request.SubscriptionId,
            ResourceGroupName = request.ResourceGroupName,
            ResourceName = request.ResourceName
        };
        
        dataFactory.EnsureDataAnnotationsValidated();
        
        dbContext.DataFactories.Add(dataFactory);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return dataFactory;
    }
}