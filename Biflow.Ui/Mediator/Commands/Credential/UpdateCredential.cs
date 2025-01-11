namespace Biflow.Ui;

public record UpdateCredentialCommand(Credential Credential) : IRequest;

internal class UpdateCredentialCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateCredentialCommand>
{
    public async Task Handle(UpdateCredentialCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var cred = await context.Credentials
            .FirstOrDefaultAsync(c => c.CredentialId == request.Credential.CredentialId, cancellationToken)
            ?? throw new NotFoundException<Credential>(request.Credential.CredentialId);
        context.Entry(cred).CurrentValues.SetValues(request.Credential);
        await context.SaveChangesAsync(cancellationToken);
    }
}