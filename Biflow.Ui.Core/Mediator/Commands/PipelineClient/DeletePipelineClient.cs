namespace Biflow.Ui.Core;

public record DeletePipelineClientCommand(Guid PipelineClientId) : IRequest;

internal class DeletePipelineClientCommandHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<DeletePipelineClientCommand>
{
    public async Task Handle(DeletePipelineClientCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var client = await context.PipelineClients
            .FirstOrDefaultAsync(p => p.PipelineClientId == request.PipelineClientId, cancellationToken);
        if (client is not null)
        {
            context.PipelineClients.Remove(client);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}