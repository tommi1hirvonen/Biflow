using BC = BCrypt.Net.BCrypt;

namespace Biflow.Ui;

public record CreateUserCommand(User User, PasswordModel PasswordModel) : IRequest;

internal class CreateUserCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    AuthenticationMethodResolver authenticationResolver)
    : IRequestHandler<CreateUserCommand>
{
    public async Task Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var jobIds = request.User.Jobs
            .Select(j => j.JobId)
            .ToArray();
        var dataTableIds = request.User.DataTables
            .Select(dt => dt.DataTableId)
            .ToArray();

        var jobs = await context.Jobs
            .Where(j => jobIds.Contains(j.JobId))
            .ToArrayAsync(cancellationToken);
        var dataTables = await context.MasterDataTables
            .Where(t => dataTableIds.Contains(t.DataTableId))
            .ToArrayAsync(cancellationToken);

        request.User.Jobs.Clear();
        request.User.DataTables.Clear();

        foreach (var job in jobs)
        {
            request.User.Jobs.Add(job);
        }
        foreach (var dataTable in dataTables)
        {
            request.User.DataTables.Add(dataTable);
        }
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