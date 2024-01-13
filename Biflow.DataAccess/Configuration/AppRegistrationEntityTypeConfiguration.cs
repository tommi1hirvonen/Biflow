using Biflow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class AppRegistrationEntityTypeConfiguration : IEntityTypeConfiguration<AppRegistration>
{
    public void Configure(EntityTypeBuilder<AppRegistration> builder)
    {
        builder.ToTable("AppRegistration")
            .HasKey(x => x.AppRegistrationId);

        builder.Property(x => x.TenantId).IsUnicode(false);
        builder.Property(x => x.ClientId).IsUnicode(false);
        builder.Property(x => x.ClientSecret).IsUnicode(false);
    }
}
