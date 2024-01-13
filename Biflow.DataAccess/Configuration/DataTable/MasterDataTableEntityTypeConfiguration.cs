namespace Biflow.DataAccess.Configuration;

internal class MasterDataTableEntityTypeConfiguration(AppDbContext context)
    : IEntityTypeConfiguration<MasterDataTable>
{
    public void Configure(EntityTypeBuilder<MasterDataTable> builder)
    {
        builder.ToTable("DataTable")
            .HasKey(x => x.DataTableId);

        builder.Property(x => x.CategoryId)
            .HasColumnName("DataTableCategoryId");

        builder.Property(x => x.Timestamp).IsRowVersion();

        builder.Property(x => x.TargetSchemaName)
            .IsUnicode(false);
        builder.Property(x => x.TargetTableName)
            .IsUnicode(false);
        builder.Property(x => x.LockedColumns)
            .HasMaxLength(8000)
            .IsUnicode(false);
        builder.Property(x => x.HiddenColumns)
            .HasMaxLength(8000)
            .IsUnicode(false);
        builder.Property(x => x.ColumnOrder)
            .HasMaxLength(8000)
            .IsUnicode(false);

        builder.HasMany(t => t.Lookups).WithOne(l => l.Table);
        builder.HasOne(t => t.Category)
        .WithMany(c => c.Tables)
        .OnDelete(DeleteBehavior.SetNull);
        
        // The user is either admin or editor or is granted authorization to the data table.
        builder.HasQueryFilter(t =>
            context.UserRoles == null ||
            context.UserRoles.Contains(Roles.Admin) ||
            context.UserRoles.Contains(Roles.DataTableMaintainer) ||
            context.Users.Any(u => u.Username == context.Username && u.AuthorizeAllDataTables) ||
            t.Users.Any(u => u.Username == context.Username)
        );
    }
}
