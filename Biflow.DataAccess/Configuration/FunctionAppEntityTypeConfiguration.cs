using Biflow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class FunctionAppEntityTypeConfiguration : IEntityTypeConfiguration<FunctionApp>
{
    public void Configure(EntityTypeBuilder<FunctionApp> builder)
    {
        builder.ToTable("FunctionApp")
            .HasKey(x => x.FunctionAppId);

        builder.Property(x => x.FunctionAppKey).IsUnicode(false);
    }
}
