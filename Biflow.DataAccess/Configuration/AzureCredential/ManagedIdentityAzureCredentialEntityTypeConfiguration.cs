namespace Biflow.DataAccess.Configuration;

public class ManagedIdentityAzureCredentialEntityTypeConfiguration
    : IEntityTypeConfiguration<ManagedIdentityAzureCredential>
{
    public void Configure(EntityTypeBuilder<ManagedIdentityAzureCredential> builder)
    {
        builder.Property(x => x.ClientId).HasColumnName("ClientId").IsUnicode(false);
    }
}