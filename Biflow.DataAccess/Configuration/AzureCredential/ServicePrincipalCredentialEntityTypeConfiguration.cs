namespace Biflow.DataAccess.Configuration;

public class ServicePrincipalCredentialEntityTypeConfiguration : IEntityTypeConfiguration<ServicePrincipalCredential>
{
    public void Configure(EntityTypeBuilder<ServicePrincipalCredential> builder)
    {
        builder.Property(x => x.ClientSecret).IsUnicode(false);
    }
}