namespace Biflow.Ui.Core;

/// <summary>
/// 
/// </summary>
/// <param name="CredentialId"></param>
/// <param name="Domain"></param>
/// <param name="Username"></param>
/// <param name="Password">Pass null to retain the previous Password value</param>
public record UpdateCredentialCommand(
    Guid CredentialId,
    string? Domain,
    string Username,
    string? Password) : IRequest<Credential>;

[UsedImplicitly]
internal class UpdateCredentialCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateCredentialCommand, Credential>
{
    public async Task<Credential> Handle(UpdateCredentialCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var credential = await dbContext.Credentials
            .FirstOrDefaultAsync(x => x.CredentialId == request.CredentialId, cancellationToken)
            ?? throw new NotFoundException<Credential>(request.CredentialId);
        credential.Domain = request.Domain;
        credential.Username = request.Username;
        if (request.Password is not null)
        {
            credential.Password = request.Password.Length == 0
                ? null 
                : request.Password;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return credential;
    }
}