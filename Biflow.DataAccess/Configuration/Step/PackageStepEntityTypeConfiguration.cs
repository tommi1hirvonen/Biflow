namespace Biflow.DataAccess.Configuration;

internal class PackageStepEntityTypeConfiguration : IEntityTypeConfiguration<PackageStep>
{
    public void Configure(EntityTypeBuilder<PackageStep> builder)
    {
        builder.Property(x => x.TimeoutMinutes).HasColumnName("TimeoutMinutes");
        builder.Property(x => x.ConnectionId).HasColumnName("ConnectionId");
    }
}
