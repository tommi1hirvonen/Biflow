namespace Biflow.Ui;

public record DeleteFunctionAppCommand(Guid FunctionAppId) : IRequest;

internal class DeleteFunctionAppCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteFunctionAppCommand>
{
    public async Task Handle(DeleteFunctionAppCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var client = await context.FunctionApps
            .FirstOrDefaultAsync(p => p.FunctionAppId == request.FunctionAppId, cancellationToken);
        if (client is not null)
        {
            context.FunctionApps.Remove(client);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}