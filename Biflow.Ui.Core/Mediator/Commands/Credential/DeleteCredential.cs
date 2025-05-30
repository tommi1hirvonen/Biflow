﻿namespace Biflow.Ui.Core;

public record DeleteCredentialCommand(Guid CredentialId) : IRequest;

[UsedImplicitly]
internal class DeleteCredentialCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteCredentialCommand>
{
    public async Task Handle(DeleteCredentialCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var cred = await context.Credentials
            .FirstOrDefaultAsync(c => c.CredentialId == request.CredentialId, cancellationToken)
            ?? throw new NotFoundException<Credential>(request.CredentialId);
        context.Credentials.Remove(cred);
        await context.SaveChangesAsync(cancellationToken);
    }
}
