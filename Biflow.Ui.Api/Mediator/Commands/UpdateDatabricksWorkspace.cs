namespace Biflow.Ui.Api.Mediator.Commands;

/// <summary>
/// 
/// </summary>
/// <param name="WorkspaceId"></param>
/// <param name="WorkspaceName"></param>
/// <param name="WorkspaceUrl"></param>
/// <param name="ApiToken">Pass null to retain the previous ApiToken value</param>
public record UpdateDatabricksWorkspaceCommand(
    Guid WorkspaceId,
    string WorkspaceName,
    string WorkspaceUrl,
    string? ApiToken) : IRequest<DatabricksWorkspace>;

[UsedImplicitly]
internal class UpdateDatabricksWorkspaceCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateDatabricksWorkspaceCommand, DatabricksWorkspace>
{
    public async Task<DatabricksWorkspace> Handle(UpdateDatabricksWorkspaceCommand request,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var workspace = await dbContext.DatabricksWorkspaces
            .FirstOrDefaultAsync(x => x.WorkspaceId == request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException<DatabricksWorkspace>(request.WorkspaceId);
        workspace.WorkspaceName = request.WorkspaceName;
        workspace.WorkspaceUrl = request.WorkspaceUrl;
        if (request.ApiToken is not null)
        {
            workspace.ApiToken = request.ApiToken;
        }
        workspace.EnsureDataAnnotationsValidated();
        await dbContext.SaveChangesAsync(cancellationToken);
        return workspace;
    }
}