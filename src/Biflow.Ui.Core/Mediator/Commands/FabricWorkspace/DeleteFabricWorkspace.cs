namespace Biflow.Ui.Core;

public record DeleteFabricWorkspaceCommand(Guid FabricWorkspaceId) : IRequest;

[UsedImplicitly]
internal class DeleteFabricWorkspaceCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteFabricWorkspaceCommand>
{
    public async Task Handle(DeleteFabricWorkspaceCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var workspace = await context.FabricWorkspaces
            .FirstOrDefaultAsync(p => p.FabricWorkspaceId == request.FabricWorkspaceId, cancellationToken)
                        ?? throw new NotFoundException<FabricWorkspace>(request.FabricWorkspaceId);
        context.FabricWorkspaces.Remove(workspace);
        await context.SaveChangesAsync(cancellationToken);
    }
}