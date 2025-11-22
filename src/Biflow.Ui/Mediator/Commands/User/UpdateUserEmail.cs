using JetBrains.Annotations;

namespace Biflow.Ui.Mediator.Commands.User;

public record UpdateUserEmailCommand(Guid UserId, string? Email) : IRequest;

[UsedImplicitly]
internal class UpdateUserEmailCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateUserEmailCommand>
{
    public async Task Handle(UpdateUserEmailCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken)
            ?? throw new NotFoundException<Biflow.Core.Entities.User>(request.UserId);
        user.Email = request.Email;
        user.EnsureDataAnnotationsValidated();
        await context.SaveChangesAsync(cancellationToken);
    }
}