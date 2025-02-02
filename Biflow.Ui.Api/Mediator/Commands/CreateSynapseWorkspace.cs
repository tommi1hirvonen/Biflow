namespace Biflow.Ui.Api.Mediator.Commands;

public record CreateSynapseWorkspaceCommand(
    string PipelineClientName,
    int MaxConcurrentPipelineSteps,
    Guid AzureCredentialId,
    string SynapseWorkspaceUrl) : IRequest<SynapseWorkspace>;

[UsedImplicitly]
internal class CreateSynapseWorkspaceCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateSynapseWorkspaceCommand, SynapseWorkspace>
{
    public async Task<SynapseWorkspace> Handle(CreateSynapseWorkspaceCommand request,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        if (!await dbContext.AzureCredentials
                .AnyAsync(x => x.AzureCredentialId == request.AzureCredentialId, cancellationToken))
        {
            throw new NotFoundException<AzureCredential>(request.AzureCredentialId);
        }

        var synapseWorkspace = new SynapseWorkspace
        {
            PipelineClientName = request.PipelineClientName,
            MaxConcurrentPipelineSteps = request.MaxConcurrentPipelineSteps,
            AzureCredentialId = request.AzureCredentialId,
            SynapseWorkspaceUrl = request.SynapseWorkspaceUrl
        };
        
        synapseWorkspace.EnsureDataAnnotationsValidated();
        
        dbContext.SynapseWorkspaces.Add(synapseWorkspace);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return synapseWorkspace;
    }
}