namespace Biflow.DataAccess.Configuration;

internal class SqlConnectionEntityTypeConfiguration : IEntityTypeConfiguration<SqlConnectionBase>
{
    public void Configure(EntityTypeBuilder<SqlConnectionBase> builder)
    {
        builder.ToTable("SqlConnection")
            .HasKey(x => x.ConnectionId);

        builder.Ignore(x => x.Steps);

        builder.Property(x => x.ConnectionString)
            .HasMaxLength(-1)
            .IsUnicode();

        builder.HasDiscriminator<SqlConnectionType>("SqlConnectionType")
            .HasValue<MsSqlConnection>(SqlConnectionType.MsSql)
            .HasValue<SnowflakeConnection>(SqlConnectionType.Snowflake);
    }
}
