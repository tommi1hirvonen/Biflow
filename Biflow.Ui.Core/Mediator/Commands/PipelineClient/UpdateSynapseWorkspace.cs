namespace Biflow.Ui.Core;

public record UpdateSynapseWorkspaceCommand(
    Guid PipelineClientId,
    string PipelineClientName,
    int MaxConcurrentPipelineSteps,
    Guid AzureCredentialId,
    string SynapseWorkspaceUrl) : IRequest<SynapseWorkspace>;

[UsedImplicitly]
internal class UpdateSynapseWorkspaceCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateSynapseWorkspaceCommand, SynapseWorkspace>
{
    public async Task<SynapseWorkspace> Handle(UpdateSynapseWorkspaceCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var synapseWorkspace = await dbContext.SynapseWorkspaces
            .FirstOrDefaultAsync(x => x.PipelineClientId == request.PipelineClientId, cancellationToken)
            ?? throw new NotFoundException<SynapseWorkspace>(request.PipelineClientId);

        if (!await dbContext.AzureCredentials
                .AnyAsync(x => x.AzureCredentialId == request.AzureCredentialId, cancellationToken))
        {
            throw new NotFoundException<AzureCredential>(request.AzureCredentialId);
        }
        
        synapseWorkspace.PipelineClientName = request.PipelineClientName;
        synapseWorkspace.MaxConcurrentPipelineSteps = request.MaxConcurrentPipelineSteps;
        synapseWorkspace.AzureCredentialId = request.AzureCredentialId;
        synapseWorkspace.SynapseWorkspaceUrl = request.SynapseWorkspaceUrl;
        
        synapseWorkspace.EnsureDataAnnotationsValidated();
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return synapseWorkspace;
    }
}