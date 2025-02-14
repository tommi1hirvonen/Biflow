using System.ComponentModel.DataAnnotations;
using BC = BCrypt.Net.BCrypt;

namespace Biflow.Ui.Api.Mediator.Commands;

public record CreateUserCommand(
    string Username,
    string? Email,
    bool AuthorizeAllJobs,
    bool AuthorizeAllDataTables,
    Guid[] AuthorizedJobIds,
    Guid[] AuthorizedDataTableIds,
    UserRole MainRole,
    bool IsSettingsEditor,
    bool IsDataTableMaintainer,
    bool IsVersionManager,
    [property: ComplexPassword]
    string? Password) : IRequest<User>;

[UsedImplicitly]
internal class CreateUserCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    AuthenticationMethodResolver authenticationResolver)
    : IRequestHandler<CreateUserCommand, User>
{
    public async Task<User> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

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

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            AuthorizeAllJobs = request.AuthorizeAllJobs,
            AuthorizeAllDataTables = request.AuthorizeAllDataTables
        };

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

        foreach (var job in jobs)
        {
            user.Jobs.Add(job);
        }
        foreach (var dataTable in dataTables)
        {
            user.DataTables.Add(dataTable);
        }
        
        user.EnsureDataAnnotationsValidated();
        
        dbContext.Users.Add(user);

        if (authenticationResolver.AuthenticationMethod == AuthenticationMethod.BuiltIn)
        {
            // Ensure password meets ComplexPasswordAttribute requirements
            request.EnsureDataAnnotationsValidated();
            var hash = BC.HashPassword(request.Password);
            dbContext.Entry(user).Property("PasswordHash").CurrentValue = hash;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }
}