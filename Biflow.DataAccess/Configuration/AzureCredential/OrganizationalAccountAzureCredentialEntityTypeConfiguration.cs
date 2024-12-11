namespace Biflow.DataAccess.Configuration;

public class OrganizationalAccountAzureCredentialEntityTypeConfiguration
    : IEntityTypeConfiguration<OrganizationalAccountAzureCredential>
{
    public void Configure(EntityTypeBuilder<OrganizationalAccountAzureCredential> builder)
    {
        builder.Property(x => x.TenantId).HasColumnName("TenantId").IsUnicode(false);
        builder.Property(x => x.ClientId).HasColumnName("ClientId").IsUnicode(false);
    }
}