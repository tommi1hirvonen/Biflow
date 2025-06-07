namespace Biflow.DataAccess.Configuration;

public class ServicePrincipalAzureCredentialEntityTypeConfiguration
    : IEntityTypeConfiguration<ServicePrincipalAzureCredential>
{
    public void Configure(EntityTypeBuilder<ServicePrincipalAzureCredential> builder)
    {
        builder.Property(x => x.TenantId).HasColumnName("TenantId").IsUnicode(false);
        builder.Property(x => x.ClientId).HasColumnName("ClientId").IsUnicode(false);
        builder.Property(x => x.ClientSecret).HasColumnName("ClientSecret").IsUnicode(false);
    }
}