namespace Biflow.Ui.Api.Mediator.Commands;

public record CreateDatabricksWorkspaceCommand(
    string WorkspaceName,
    string WorkspaceUrl,
    string ApiToken) : IRequest<DatabricksWorkspace>;

[UsedImplicitly]
internal class CreateDatabricksWorkspaceCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateDatabricksWorkspaceCommand, DatabricksWorkspace>
{
    public async Task<DatabricksWorkspace> Handle(CreateDatabricksWorkspaceCommand request,
        CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var workspace = new DatabricksWorkspace
        {
            WorkspaceName = request.WorkspaceName,
            WorkspaceUrl = request.WorkspaceUrl,
            ApiToken = request.ApiToken
        };
        workspace.EnsureDataAnnotationsValidated();
        context.DatabricksWorkspaces.Add(workspace);
        await context.SaveChangesAsync(cancellationToken);
        return workspace;
    }
}