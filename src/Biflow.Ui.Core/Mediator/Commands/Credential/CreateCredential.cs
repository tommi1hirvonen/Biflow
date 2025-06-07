namespace Biflow.Ui.Core;

public record CreateCredentialCommand(
    string? Domain,
    string Username,
    string? Password) : IRequest<Credential>;

[UsedImplicitly]
internal class CreateCredentialCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<CreateCredentialCommand, Credential>
{
    public async Task<Credential> Handle(CreateCredentialCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var credential = new Credential
        {
            Domain = request.Domain,
            Username = request.Username,
            Password = request.Password
        };
        credential.EnsureDataAnnotationsValidated();
        dbContext.Credentials.Add(credential);
        await dbContext.SaveChangesAsync(cancellationToken);
        return credential;
    }
}