namespace Biflow.Ui.Core;

public record CreateFabricWorkspaceCommand(
    string FabricWorkspaceName,
    Guid WorkspaceId,
    Guid AzureCredentialId) : IRequest<FabricWorkspace>;

[UsedImplicitly]
internal class CreateFabricWorkspaceCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateFabricWorkspaceCommand, FabricWorkspace>
{
    public async Task<FabricWorkspace> Handle(CreateFabricWorkspaceCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        if (!await dbContext.AzureCredentials
                .AnyAsync(x => x.AzureCredentialId == request.AzureCredentialId, cancellationToken))
        {
            throw new NotFoundException<AzureCredential>(request.AzureCredentialId);
        }
        
        var workspace = new FabricWorkspace
        {
            FabricWorkspaceName = request.FabricWorkspaceName,
            WorkspaceId = request.WorkspaceId,
            AzureCredentialId = request.AzureCredentialId
        };
        workspace.EnsureDataAnnotationsValidated();
        dbContext.FabricWorkspaces.Add(workspace);
        await dbContext.SaveChangesAsync(cancellationToken);
        return workspace;
    }
}