
namespace Biflow.DataAccess.Configuration;
internal class CredentialEntityTypeConfiguration : IEntityTypeConfiguration<Credential>
{
    public void Configure(EntityTypeBuilder<Credential> builder)
    {
        builder.ToTable("Credential")
            .HasKey(c => c.CredentialId);

        builder.Property(x => x.Domain)
            .HasMaxLength(200);
        builder.Property(x => x.Username)
            .HasMaxLength(200);
        builder.Property(x => x.Password)
            .HasMaxLength(200);
    }
}
