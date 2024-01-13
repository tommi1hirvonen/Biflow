namespace Biflow.DataAccess.Configuration;

internal class MasterDataTableLookupEntityTypeConfiguration : IEntityTypeConfiguration<MasterDataTableLookup>
{
    public void Configure(EntityTypeBuilder<MasterDataTableLookup> builder)
    {
        // Use client cascade because of multiple cascade paths not supported by SQL Server.
        builder.HasOne(l => l.Table).WithMany(t => t.Lookups).OnDelete(DeleteBehavior.ClientCascade);
        builder.HasOne(l => l.LookupTable).WithMany(t => t.DependentLookups).OnDelete(DeleteBehavior.ClientCascade);
        builder.HasIndex(x => new { x.TableId, x.ColumnName }, "UQ_DataTableLookup").IsUnique();
    }
}
