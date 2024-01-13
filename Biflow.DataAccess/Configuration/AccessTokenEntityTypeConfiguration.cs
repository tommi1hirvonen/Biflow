using Biflow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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
        builder.HasOne(x => x.AppRegistration)
            .WithMany(x => x.AccessTokens)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
