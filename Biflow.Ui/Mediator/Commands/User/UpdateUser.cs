namespace Biflow.Ui;

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
        
        var dataTableIds = request.User.DataTables
            .Select(x => x.DataTableId)
            .ToArray();
        var dataTables = await context.MasterDataTables
            .Where(x => dataTableIds.Contains(x.DataTableId))
            .ToArrayAsync(cancellationToken);
        
        var jobIds = request.User.Jobs
            .Select(x => x.JobId)
            .ToArray();
        var jobs = await context.Jobs
            .Where(x => jobIds.Contains(x.JobId))
            .ToArrayAsync(cancellationToken);

        context.Entry(user).CurrentValues.SetValues(request.User);
        
        context.MergeCollections(
            user.DataTables,
            dataTables,
            d => d.DataTableId,
            updateMatchingItemValues: false);

        context.MergeCollections(
            user.Jobs,
            jobs,
            j => j.JobId,
            updateMatchingItemValues: false);

        await context.SaveChangesAsync(cancellationToken);
    }
}