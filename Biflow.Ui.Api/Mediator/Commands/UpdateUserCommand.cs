namespace Biflow.Ui.Api.Mediator.Commands;

public record UpdateUserCommand(
    Guid UserId,
    string Username,
    string? Email,
    bool AuthorizeAllJobs,
    bool AuthorizeAllDataTables,
    Guid[] AuthorizedJobIds,
    Guid[] AuthorizedDataTableIds,
    UserRole MainRole,
    bool IsSettingsEditor,
    bool IsDataTableMaintainer,
    bool IsVersionManager) : IRequest<User>;

[UsedImplicitly]
internal class UpdateUserCommandHandler(IDbContextFactory<AppDbContext> dbContextFactory)
    : IRequestHandler<UpdateUserCommand, User>
{
    public async Task<User> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var user = await dbContext.Users
            .Include(u => u.Jobs)
            .Include(u => u.DataTables)
            .FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken)
            ?? throw new NotFoundException<User>(request.UserId);
        
        var jobs = await dbContext.Jobs
            .Where(j => request.AuthorizedJobIds.Contains(j.JobId))
            .ToArrayAsync(cancellationToken);

        foreach (var id in request.AuthorizedJobIds)
        {
            if (jobs.All(j => j.JobId != id))
            {
                throw new NotFoundException<Job>(id);
            }
        }
        
        var dataTables = await dbContext.MasterDataTables
            .Where(t => request.AuthorizedDataTableIds.Contains(t.DataTableId))
            .ToArrayAsync(cancellationToken);
        
        foreach (var id in request.AuthorizedDataTableIds)
        {
            if (dataTables.All(t => t.DataTableId != id))
            {
                throw new NotFoundException<MasterDataTable>(id);
            }
        }
        
        dbContext.MergeCollections(
            user.DataTables,
            dataTables,
            d => d.DataTableId,
            updateMatchingItemValues: false);

        dbContext.MergeCollections(
            user.Jobs,
            jobs,
            j => j.JobId,
            updateMatchingItemValues: false);
        
        switch (request.MainRole)
        {
            case UserRole.Admin:
                user.SetIsAdmin();
                break;
            case UserRole.Editor:
                user.SetIsEditor();
                break;
            case UserRole.Operator:
                user.SetIsOperator();
                break;
            case UserRole.Viewer:
                user.SetIsViewer();
                break;
            default:
                throw new ArgumentOutOfRangeException($"Unrecognized role {request.MainRole}");
        }
        
        user.SetIsSettingsEditor(request.IsSettingsEditor);
        user.SetIsDataTableMaintainer(request.IsDataTableMaintainer);
        user.SetIsVersionManager(request.IsVersionManager);

        user.Username = request.Username;
        user.Email = request.Email;
        user.AuthorizeAllJobs = request.AuthorizeAllJobs;
        user.AuthorizeAllDataTables = request.AuthorizeAllDataTables;
        
        user.EnsureDataAnnotationsValidated();

        await dbContext.SaveChangesAsync(cancellationToken);

        return user;
    }
}