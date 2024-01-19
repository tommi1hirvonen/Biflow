namespace Biflow.Ui.Core;

public record CreatePipelineClientCommand(PipelineClient Client) : IRequest;

internal class CreatePipelineClientCommandHandler(IDbContextFactory<ServiceDbContext> dbContextFactory)
    : IRequestHandler<CreatePipelineClientCommand>
{
    public async Task Handle(CreatePipelineClientCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.PipelineClients.Add(request.Client);
        await context.SaveChangesAsync(cancellationToken);
    }
}