namespace Biflow.Ui.Core;

public record DeleteDatabricksWorkspaceCommand(Guid WorkspaceId) : IRequest;

internal class DeleteDatabricksWorkspaceCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory) : IRequestHandler<DeleteDatabricksWorkspaceCommand>
{
    public async Task Handle(DeleteDatabricksWorkspaceCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var workspace = await context.DatabricksWorkspaces
            .FirstOrDefaultAsync(c => c.WorkspaceId == request.WorkspaceId, cancellationToken);
        if (workspace is not null)
        {
            context.DatabricksWorkspaces.Remove(workspace);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}