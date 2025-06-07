namespace Biflow.Ui.Core;

public record DeleteFunctionAppCommand(Guid FunctionAppId) : IRequest;

[UsedImplicitly]
internal class DeleteFunctionAppCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteFunctionAppCommand>
{
    public async Task Handle(DeleteFunctionAppCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var client = await context.FunctionApps
            .FirstOrDefaultAsync(p => p.FunctionAppId == request.FunctionAppId, cancellationToken)
            ?? throw new NotFoundException<FunctionApp>(request.FunctionAppId);
        context.FunctionApps.Remove(client);
        await context.SaveChangesAsync(cancellationToken);
    }
}