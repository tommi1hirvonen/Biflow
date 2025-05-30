﻿namespace Biflow.Ui.Core;

public record DeleteUserCommand(Guid UserId) : IRequest;

[UsedImplicitly]
internal class DeleteUserCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<DeleteUserCommand>
{
    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken)
            ?? throw new NotFoundException<User>(request.UserId);
        context.Users.Remove(user);
        await context.SaveChangesAsync(cancellationToken);
    }
}