namespace Biflow.Ui.Core;

public record CreatePipelineClientCommand(PipelineClient Client) : IRequest;

internal class CreatePipelineClientCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreatePipelineClientCommand>
{
    public async Task Handle(CreatePipelineClientCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.PipelineClients.Add(request.Client);
        await context.SaveChangesAsync(cancellationToken);
    }
}