
namespace Biflow.DataAccess.Configuration;

internal class SnowflakeConnectionEntityTypeConfiguration : IEntityTypeConfiguration<SnowflakeConnection>
{
    public void Configure(EntityTypeBuilder<SnowflakeConnection> builder)
    {
        builder.Property(x => x.MaxConcurrentSqlSteps).HasColumnName("MaxConcurrentSqlSteps");
    }
}
