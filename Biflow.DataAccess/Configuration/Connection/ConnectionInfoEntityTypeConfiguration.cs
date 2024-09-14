namespace Biflow.DataAccess.Configuration;

internal class ConnectionInfoEntityTypeConfiguration : IEntityTypeConfiguration<ConnectionBase>
{
    public void Configure(EntityTypeBuilder<ConnectionBase> builder)
    {
        builder.ToTable("Connection")
            .HasKey(x => x.ConnectionId);

        builder.Ignore(x => x.Steps);

        builder.HasDiscriminator<ConnectionType>("ConnectionType")
            .HasValue<MsSqlConnection>(ConnectionType.Sql)
            .HasValue<AnalysisServicesConnection>(ConnectionType.AnalysisServices);
    }
}
