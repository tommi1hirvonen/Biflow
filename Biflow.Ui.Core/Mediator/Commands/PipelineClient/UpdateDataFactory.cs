namespace Biflow.Ui.Core;

public record UpdateDataFactoryCommand(
    Guid PipelineClientId,
    string PipelineClientName,
    int MaxConcurrentPipelineSteps,
    Guid AzureCredentialId,
    string SubscriptionId,
    string ResourceGroupName,
    string ResourceName) : IRequest<DataFactory>;

[UsedImplicitly]
internal class UpdateDataFactoryCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateDataFactoryCommand, DataFactory>
{
    public async Task<DataFactory> Handle(UpdateDataFactoryCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var dataFactory = await dbContext.DataFactories
            .FirstOrDefaultAsync(x => x.PipelineClientId == request.PipelineClientId, cancellationToken)
            ?? throw new NotFoundException<DataFactory>(request.PipelineClientId);

        if (!await dbContext.AzureCredentials
                .AnyAsync(x => x.AzureCredentialId == request.AzureCredentialId, cancellationToken))
        {
            throw new NotFoundException<AzureCredential>(request.AzureCredentialId);
        }
        
        dataFactory.PipelineClientName = request.PipelineClientName;
        dataFactory.MaxConcurrentPipelineSteps = request.MaxConcurrentPipelineSteps;
        dataFactory.AzureCredentialId = request.AzureCredentialId;
        dataFactory.SubscriptionId = request.SubscriptionId;
        dataFactory.ResourceGroupName = request.ResourceGroupName;
        dataFactory.ResourceName = request.ResourceName;
        
        dataFactory.EnsureDataAnnotationsValidated();
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return dataFactory;
    }
}