namespace Biflow.Ui.Core;

public record CreateCredentialCommand(Credential Credential) : IRequest;

internal class CreateCredentialCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateCredentialCommand>
{
    public async Task Handle(CreateCredentialCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Credentials.Add(request.Credential);
        await context.SaveChangesAsync(cancellationToken);
    }
}