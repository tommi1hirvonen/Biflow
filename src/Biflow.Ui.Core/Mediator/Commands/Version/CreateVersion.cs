using Biflow.Ui.Core.Projection;

namespace Biflow.Ui.Core;

public record CreateVersionCommand(string? Description, Guid? PropertyTranslationSetId) : IRequest<VersionProjection>;

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
        IReadOnlyList<PropertyTranslation> propertyTranslations;
        await using (var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            propertyTranslations = await ctx.PropertyTranslations
                .Where(t => t.PropertyTranslationSetId == request.PropertyTranslationSetId)
                .OrderBy(t => t.Order)
                .ToArrayAsync(cancellationToken);
        }
        var version = new EnvironmentVersion
        {
            Snapshot = snapshot.ToJson(preserveReferences: false, propertyTranslations),
            SnapshotWithReferencesPreserved = snapshot.ToJson(preserveReferences: true, propertyTranslations),
            Description = request.Description,
            CreatedOn = DateTimeOffset.Now,
            CreatedBy = username
        };
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        context.EnvironmentVersions.Add(version);
        await context.SaveChangesAsync(cancellationToken);
        return new VersionProjection(version.VersionId, version.Description, version.CreatedOn, version.CreatedBy);
    }
}