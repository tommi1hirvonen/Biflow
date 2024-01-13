namespace Biflow.DataAccess.Configuration;

internal class DataObjectEntityTypeConfiguration : IEntityTypeConfiguration<DataObject>
{
    public void Configure(EntityTypeBuilder<DataObject> builder)
    {
        builder.ToTable("DataObject");

        builder.Property(x => x.ObjectUri).IsUnicode(false);

        builder.HasIndex(p => p.ObjectUri, "UQ_DataObject")
            .IsUnique();
    }
}
