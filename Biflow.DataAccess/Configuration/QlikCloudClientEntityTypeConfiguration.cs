using Biflow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class QlikCloudClientEntityTypeConfiguration : IEntityTypeConfiguration<QlikCloudClient>
{
    public void Configure(EntityTypeBuilder<QlikCloudClient> builder)
    {
        builder.HasMany(c => c.Steps)
            .WithOne(s => s.QlikCloudClient);
    }
}
