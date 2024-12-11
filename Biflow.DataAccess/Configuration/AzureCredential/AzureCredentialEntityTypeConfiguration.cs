namespace Biflow.DataAccess.Configuration;

internal class AzureCredentialEntityTypeConfiguration : IEntityTypeConfiguration<AzureCredential>
{
    public void Configure(EntityTypeBuilder<AzureCredential> builder)
    {
        builder.ToTable("AzureCredential")
            .HasKey(x => x.AzureCredentialId);
        builder.Property(x => x.AzureCredentialType)
            .IsRequired()
            .HasDefaultValue(AzureCredentialType.ServicePrincipal);
        builder.HasDiscriminator<AzureCredentialType>("AzureCredentialType")
            .HasValue<ServicePrincipalAzureCredential>(AzureCredentialType.ServicePrincipal)
            .HasValue<OrganizationalAccountAzureCredential>(AzureCredentialType.OrganizationalAccount)
            .HasValue<ManagedIdentityAzureCredential>(AzureCredentialType.ManagedIdentity);
    }
}
