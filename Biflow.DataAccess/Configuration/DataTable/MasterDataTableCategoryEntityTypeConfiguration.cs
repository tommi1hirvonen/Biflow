namespace Biflow.DataAccess.Configuration;

internal class MasterDataTableCategoryEntityTypeConfiguration : IEntityTypeConfiguration<MasterDataTableCategory>
{
    public void Configure(EntityTypeBuilder<MasterDataTableCategory> builder)
    {
        builder.ToTable("DataTableCategory")
            .HasKey(x => x.CategoryId);

        builder.Property(x => x.CategoryId).HasColumnName("DataTableCategoryId");
        builder.Property(x => x.CategoryName).HasColumnName("DataTableCategoryName");

        builder.HasIndex(p => p.CategoryName, "UQ_DataTableCategory")
            .IsUnique();
    }
}
