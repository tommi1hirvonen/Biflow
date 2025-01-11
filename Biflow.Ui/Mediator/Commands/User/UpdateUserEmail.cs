namespace Biflow.Ui;

public record UpdateUserEmailCommand(Guid UserId, string? Email) : IRequest;

internal class UpdateUserEmailCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateUserEmailCommand>
{
    public async Task Handle(UpdateUserEmailCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await context.Users
            .Where(u => u.UserId == request.UserId)
            .ExecuteUpdateAsync(x => x.SetProperty(u => u.Email, request.Email), cancellationToken);
    }
}