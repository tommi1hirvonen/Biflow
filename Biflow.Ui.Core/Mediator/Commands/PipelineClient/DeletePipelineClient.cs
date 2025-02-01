namespace Biflow.Ui.Core;

public record DeletePipelineClientCommand(Guid PipelineClientId) : IRequest;

[UsedImplicitly]
internal class DeletePipelineClientCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeletePipelineClientCommand>
{
    public async Task Handle(DeletePipelineClientCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var client = await context.PipelineClients
            .FirstOrDefaultAsync(p => p.PipelineClientId == request.PipelineClientId, cancellationToken)
            ?? throw new NotFoundException<PipelineClient>(request.PipelineClientId);
        context.PipelineClients.Remove(client);
        await context.SaveChangesAsync(cancellationToken);
    }
}