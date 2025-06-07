namespace Biflow.DataAccess.Configuration;

internal class AccessTokenEntityTypeConfiguration : IEntityTypeConfiguration<AccessToken>
{
    public void Configure(EntityTypeBuilder<AccessToken> builder)
    {
        builder.ToTable("AccessToken");
        builder.HasKey(t => new { t.AzureCredentialId, t.ResourceUrl });
        builder.Property(t => t.ResourceUrl)
            .HasMaxLength(850)
            .IsUnicode(false);
        builder.Property(t => t.Token)
            .HasMaxLength(-1)
            .IsUnicode();
        builder.HasOne(x => x.AzureCredential)
            .WithMany(x => x.AccessTokens)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
