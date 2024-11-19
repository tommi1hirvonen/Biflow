namespace Biflow.Ui.Core;

public record CreateDatabricksWorkspaceCommand(DatabricksWorkspace Workspace) : IRequest;

internal class CreateDatabricksWorkspaceCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateDatabricksWorkspaceCommand>
{
    public async Task Handle(CreateDatabricksWorkspaceCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.DatabricksWorkspaces.Add(request.Workspace);
        await context.SaveChangesAsync(cancellationToken);
    }
}