using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class MasterDataTableCategoryEntityTypeConfiguration : IEntityTypeConfiguration<MasterDataTableCategory>
{
    public void Configure(EntityTypeBuilder<MasterDataTableCategory> builder)
    {
        builder.HasIndex(p => p.CategoryName, "UQ_DataTableCategory")
            .IsUnique();
    }
}
