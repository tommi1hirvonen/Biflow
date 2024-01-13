namespace Biflow.DataAccess.Configuration;

internal class MasterDataTableCategoryEntityTypeConfiguration : IEntityTypeConfiguration<MasterDataTableCategory>
{
    public void Configure(EntityTypeBuilder<MasterDataTableCategory> builder)
    {
        builder.HasIndex(p => p.CategoryName, "UQ_DataTableCategory")
            .IsUnique();
    }
}
