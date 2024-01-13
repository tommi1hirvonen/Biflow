using BC = BCrypt.Net.BCrypt;

namespace Biflow.Ui.Core;

public record CreateUserCommand(User User, PasswordModel PasswordModel) : IRequest;

internal class CreateUserCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    AuthenticationMethodResolver authenticationResolver)
    : IRequestHandler<CreateUserCommand>
{
    public async Task Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.Users.Add(request.User);
        if (authenticationResolver.AuthenticationMethod == AuthenticationMethod.BuiltIn)
        {
            var hash = BC.HashPassword(request.PasswordModel.Password);
            context.Entry(request.User)
                .Property("PasswordHash")
                .CurrentValue = hash;
        }
        await context.SaveChangesAsync(cancellationToken);
    }
}