namespace Biflow.DataAccess.Configuration;

internal class EnvironmentVersionEntityTypeConfiguration : IEntityTypeConfiguration<EnvironmentVersion>
{
    public void Configure(EntityTypeBuilder<EnvironmentVersion> builder)
    {
        builder.ToTable("EnvironmentVersion").HasKey(x => x.VersionId);
    }
}
