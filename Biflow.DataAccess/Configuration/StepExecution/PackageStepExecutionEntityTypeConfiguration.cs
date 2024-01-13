namespace Biflow.DataAccess.Configuration;

internal class PackageStepExecutionEntityTypeConfiguration : IEntityTypeConfiguration<PackageStepExecution>
{
    public void Configure(EntityTypeBuilder<PackageStepExecution> builder)
    {
        builder.Property(x => x.TimeoutMinutes).HasColumnName("TimeoutMinutes");
        builder.Property(x => x.ConnectionId).HasColumnName("ConnectionId");
    }
}
