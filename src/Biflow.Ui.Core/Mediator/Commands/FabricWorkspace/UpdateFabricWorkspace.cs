namespace Biflow.Ui.Core;

public record UpdateFabricWorkspaceCommand(
    Guid FabricWorkspaceId,
    string FabricWorkspaceName,
    Guid WorkspaceId,
    Guid AzureCredentialId) : IRequest<FabricWorkspace>;

[UsedImplicitly]
internal class UpdateFabricWorkspaceCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateFabricWorkspaceCommand, FabricWorkspace>
{
    public async Task<FabricWorkspace> Handle(UpdateFabricWorkspaceCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var workspace = await dbContext.FabricWorkspaces
            .FirstOrDefaultAsync(x => x.FabricWorkspaceId == request.FabricWorkspaceId, cancellationToken)
                        ?? throw new NotFoundException<FabricWorkspace>(request.FabricWorkspaceId);

        if (!await dbContext.AzureCredentials
                .AnyAsync(x => x.AzureCredentialId == request.AzureCredentialId, cancellationToken))
        {
            throw new NotFoundException<AzureCredential>(request.AzureCredentialId);
        }
        
        workspace.FabricWorkspaceName = request.FabricWorkspaceName;
        workspace.AzureCredentialId = request.AzureCredentialId;
        workspace.WorkspaceId = request.WorkspaceId;
        workspace.AzureCredentialId = request.AzureCredentialId;
        
        workspace.EnsureDataAnnotationsValidated();
        
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return workspace;
    }
}