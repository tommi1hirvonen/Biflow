using Biflow.Ui.Core.Projection;

namespace Biflow.Ui.Core;

public record CreateVersionCommand(string? Description) : IRequest<VersionProjection>;

[UsedImplicitly]
internal class CreateVersionCommandHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    IUserService userService,
    EnvironmentSnapshotBuilder snapshotBuilder) : IRequestHandler<CreateVersionCommand, VersionProjection>
{
    public async Task<VersionProjection> Handle(CreateVersionCommand request, CancellationToken cancellationToken)
    {
        var username = userService.Username;
        var snapshot = await snapshotBuilder.CreateAsync();
        var version = new EnvironmentVersion
        {
            Snapshot = snapshot.ToJson(preserveReferences: false),
            SnapshotWithReferencesPreserved = snapshot.ToJson(preserveReferences: true),
            Description = request.Description,
            CreatedOn = DateTimeOffset.Now,
            CreatedBy = username
        };
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.EnvironmentVersions.Add(version);
        await context.SaveChangesAsync(cancellationToken);
        return new(version.VersionId, version.Description, version.CreatedOn, version.CreatedBy);
    }
}