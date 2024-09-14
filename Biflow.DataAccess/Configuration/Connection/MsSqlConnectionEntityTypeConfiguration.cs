namespace Biflow.DataAccess.Configuration;

internal class MsSqlConnectionEntityTypeConfiguration : IEntityTypeConfiguration<MsSqlConnection>
{
    public void Configure(EntityTypeBuilder<MsSqlConnection> builder)
    {
        builder.Property(x => x.CredentialId).HasColumnName("CredentialId");
    }
}
