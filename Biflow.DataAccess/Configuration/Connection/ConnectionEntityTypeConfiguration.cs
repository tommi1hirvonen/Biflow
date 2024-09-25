namespace Biflow.DataAccess.Configuration;

internal class ConnectionEntityTypeConfiguration : IEntityTypeConfiguration<ConnectionBase>
{
    public void Configure(EntityTypeBuilder<ConnectionBase> builder)
    {
        builder.ToTable("Connection")
            .HasKey(x => x.ConnectionId);

        builder.Ignore(x => x.Steps);

        builder.Property(x => x.ConnectionString)
            .HasMaxLength(-1)
            .IsUnicode();

        builder.HasDiscriminator<ConnectionType>("ConnectionType")
            .HasValue<MsSqlConnection>(ConnectionType.Sql)
            .HasValue<AnalysisServicesConnection>(ConnectionType.AnalysisServices)
            .HasValue<SnowflakeConnection>(ConnectionType.Snowflake);
    }
}
