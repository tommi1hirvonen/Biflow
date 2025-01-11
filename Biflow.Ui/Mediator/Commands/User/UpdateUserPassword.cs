using BC = BCrypt.Net.BCrypt;

namespace Biflow.Ui;

public record UpdateUserPasswordCommand(string Username, string OldPassword, string NewPassword) : IRequest;

internal class UpdateUserPasswordCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateUserPasswordCommand>
{
    public async Task Handle(UpdateUserPasswordCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var oldHash = await context.Users
            .Where(u => u.Username == request.Username)
            .Select(u => EF.Property<string?>(u, "PasswordHash"))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ApplicationException("User not found");
        
        var auth = BC.Verify(request.OldPassword, oldHash);
        if (!auth)
        {
            throw new ApplicationException("Incorrect old password");
        }

        var newHash = BC.HashPassword(request.NewPassword);
        await context.Users
            .Where(u => u.Username == request.Username)
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(u => EF.Property<string?>(u, "PasswordHash"), newHash), cancellationToken);
    }
}