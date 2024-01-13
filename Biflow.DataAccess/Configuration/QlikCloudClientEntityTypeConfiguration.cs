namespace Biflow.DataAccess.Configuration;

internal class QlikCloudClientEntityTypeConfiguration : IEntityTypeConfiguration<QlikCloudClient>
{
    public void Configure(EntityTypeBuilder<QlikCloudClient> builder)
    {
        builder.ToTable("QlikCloudClient")
            .HasKey(x => x.QlikCloudClientId);

        builder.HasMany(c => c.Steps)
            .WithOne(s => s.QlikCloudClient);
    }
}
