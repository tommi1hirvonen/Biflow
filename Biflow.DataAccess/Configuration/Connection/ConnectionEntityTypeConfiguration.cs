namespace Biflow.DataAccess.Configuration;

internal class ConnectionEntityTypeConfiguration : IEntityTypeConfiguration<SqlConnectionBase>
{
    public void Configure(EntityTypeBuilder<SqlConnectionBase> builder)
    {
        builder.ToTable("Connection")
            .HasKey(x => x.ConnectionId);

        builder.Ignore(x => x.Steps);

        builder.Property(x => x.ConnectionString)
            .HasMaxLength(-1)
            .IsUnicode();

        builder.HasDiscriminator<SqlConnectionType>("ConnectionType")
            .HasValue<MsSqlConnection>(SqlConnectionType.Sql)
            .HasValue<SnowflakeConnection>(SqlConnectionType.Snowflake);
    }
}
