namespace Biflow.DataAccess.Configuration;

internal class QlikCloudEnvironmentEntityTypeConfiguration : IEntityTypeConfiguration<QlikCloudEnvironment>
{
    public void Configure(EntityTypeBuilder<QlikCloudEnvironment> builder)
    {
        builder.ToTable("QlikCloudEnvironment")
            .HasKey(x => x.QlikCloudEnvironmentId);

        builder.HasMany(c => c.Steps)
            .WithOne(s => s.QlikCloudEnvironment);
    }
}
