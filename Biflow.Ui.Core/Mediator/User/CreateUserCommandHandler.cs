using Biflow.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Biflow.Ui.Core;

internal class CreateUserCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    AuthenticationMethodResolver authenticationResolver)
    : IRequestHandler<CreateUserCommand>
{
    public async Task Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        using var transaction = context.Database.BeginTransaction().GetDbTransaction();
        try
        {
            // Add user without password
            context.Users.Add(request.User);

            await context.SaveChangesAsync(cancellationToken);

            if (authenticationResolver.AuthenticationMethod == AuthenticationMethod.BuiltIn)
            {
                var connection = context.Database.GetDbConnection();

                // Update the password hash.
                await UserService.AdminUpdatePasswordAsync(request.User.Username, request.PasswordModel.Password, connection, transaction);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
