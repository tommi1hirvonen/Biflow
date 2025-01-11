namespace Biflow.Ui;

public record UpdateApiKeyCommand(ApiKey ApiKey) : IRequest;

internal class UpdateApiKeyCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateApiKeyCommand>
{
    public async Task Handle(UpdateApiKeyCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var key = await context.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == request.ApiKey.Id, cancellationToken)
            ?? throw new NotFoundException<ApiKey>(request.ApiKey.Id);
        context.Entry(key).CurrentValues.SetValues(request.ApiKey);
        await context.SaveChangesAsync(cancellationToken);
    }
}