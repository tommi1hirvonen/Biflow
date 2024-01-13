using Biflow.Core.Entities;
using Biflow.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Core;

public record UpdateUserCommand(User User) : IRequest;

internal class UpdateUserCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateUserCommand>
{
    public async Task Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var query = context.Users.AsQueryable();

        if (request.User.DataTables is not null)
        {
            query = query.Include(u => u.DataTables);
        }
        if (request.User.Jobs is not null)
        {
            query = query.Include(u => u.Jobs);
        }

        var user = await query.FirstOrDefaultAsync(u => u.UserId == request.User.UserId, cancellationToken);
        if (user is null)
        {
            return;
        }

        context.Entry(user).CurrentValues.SetValues(request.User);

        if (request.User.DataTables is not null)
        {
            context.MergeCollections(
                user.DataTables,
                request.User.DataTables,
                d => d.DataTableId,
                updateMatchingItemValues: false);
        }
        if (request.User.Jobs is not null)
        {
            context.MergeCollections(
                user.Jobs,
                request.User.Jobs,
                j => j.JobId,
                updateMatchingItemValues: false);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}