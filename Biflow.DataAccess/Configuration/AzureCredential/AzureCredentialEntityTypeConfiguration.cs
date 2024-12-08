namespace Biflow.DataAccess.Configuration;

internal class AzureCredentialEntityTypeConfiguration : IEntityTypeConfiguration<AzureCredential>
{
    public void Configure(EntityTypeBuilder<AzureCredential> builder)
    {
        builder.ToTable("AzureCredential")
            .HasKey(x => x.AzureCredentialId);

        builder.Property(x => x.TenantId).IsUnicode(false);
        builder.Property(x => x.ClientId).IsUnicode(false);
        
        builder.Property(x => x.AzureCredentialType)
            .IsRequired()
            .HasDefaultValue(AzureCredentialType.ServicePrincipal);
        builder.HasDiscriminator<AzureCredentialType>("AzureCredentialType")
            .HasValue<ServicePrincipalCredential>(AzureCredentialType.ServicePrincipal)
            .HasValue<OrganizationalAccountCredential>(AzureCredentialType.OrganizationalAccount);
    }
}
