namespace Biflow.DataAccess.Configuration;

internal class ConnectionInfoEntityTypeConfiguration : IEntityTypeConfiguration<ConnectionInfoBase>
{
    public void Configure(EntityTypeBuilder<ConnectionInfoBase> builder)
    {
        builder.HasDiscriminator<ConnectionType>("ConnectionType")
            .HasValue<SqlConnectionInfo>(ConnectionType.Sql)
            .HasValue<AnalysisServicesConnectionInfo>(ConnectionType.AnalysisServices);
    }
}
