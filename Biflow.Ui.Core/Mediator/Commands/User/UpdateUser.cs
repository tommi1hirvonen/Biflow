namespace Biflow.Ui.Core;

public record UpdateUserCommand(User User) : IRequest;

internal class UpdateUserCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateUserCommand>
{
    public async Task Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var query = context.Users
            .Include(u => u.DataTables)
            .Include(u => u.Jobs);

        var user = await query.FirstOrDefaultAsync(u => u.UserId == request.User.UserId, cancellationToken);
        if (user is null)
        {
            return;
        }

        context.Entry(user).CurrentValues.SetValues(request.User);
        
        context.MergeCollections(
            user.DataTables,
            request.User.DataTables,
            d => d.DataTableId,
            updateMatchingItemValues: false);

        context.MergeCollections(
            user.Jobs,
            request.User.Jobs,
            j => j.JobId,
            updateMatchingItemValues: false);

        await context.SaveChangesAsync(cancellationToken);
    }
}