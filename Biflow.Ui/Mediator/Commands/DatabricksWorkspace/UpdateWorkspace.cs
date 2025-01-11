namespace Biflow.Ui;

public record UpdateDatabricksWorkspaceCommand(DatabricksWorkspace Workspace) : IRequest;

internal class UpdateDatabricksWorkspaceCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateDatabricksWorkspaceCommand>
{
    public async Task Handle(UpdateDatabricksWorkspaceCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Attach(request.Workspace).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }
}