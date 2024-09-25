namespace Biflow.DataAccess.Configuration;

internal class AccessTokenEntityTypeConfiguration : IEntityTypeConfiguration<AccessToken>
{
    public void Configure(EntityTypeBuilder<AccessToken> builder)
    {
        builder.ToTable("AccessToken");
        builder.HasKey(t => new { t.AppRegistrationId, t.ResourceUrl });
        builder.Property(t => t.ResourceUrl)
            .HasMaxLength(1000)
            .IsUnicode(false);
        builder.Property(t => t.Token)
            .HasMaxLength(-1)
            .IsUnicode();
        builder.HasOne(x => x.AppRegistration)
            .WithMany(x => x.AccessTokens)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
