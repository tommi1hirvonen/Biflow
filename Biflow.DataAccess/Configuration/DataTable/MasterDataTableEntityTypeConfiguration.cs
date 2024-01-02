using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class MasterDataTableEntityTypeConfiguration(AppDbContext context)
    : IEntityTypeConfiguration<MasterDataTable>
{
    public void Configure(EntityTypeBuilder<MasterDataTable> builder)
    {
        builder.ToTable(t => t.HasTrigger("Trigger_DataTable"));
        builder.HasMany(t => t.Lookups).WithOne(l => l.Table);
        builder.HasOne(t => t.Category)
        .WithMany(c => c.Tables)
        .OnDelete(DeleteBehavior.SetNull);
        
        if (context.Username is not null)
        {
            // The user is either admin or editor or is granted authorization to the data table.
            builder.HasQueryFilter(t =>
                context.UserIsAdmin ||
                context.UserIsDataTableMaintainer ||
                context.Users.Any(u => u.Username == context.Username && u.AuthorizeAllDataTables) ||
                t.Users.Any(u => u.Username == context.Username)
            );
        }
    }
}
