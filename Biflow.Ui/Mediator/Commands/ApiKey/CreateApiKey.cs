namespace Biflow.Ui;

public record CreateApiKeyCommand(ApiKey ApiKey) : IRequest;

internal class CreateApiKeyCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateApiKeyCommand>
{
    public async Task Handle(CreateApiKeyCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.ApiKeys.Add(request.ApiKey);
        await context.SaveChangesAsync(cancellationToken);
    }
}