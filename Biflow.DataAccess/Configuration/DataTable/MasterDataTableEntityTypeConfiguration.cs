using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class MasterDataTableEntityTypeConfiguration(AppDbContext context)
    : IEntityTypeConfiguration<MasterDataTable>
{
    public void Configure(EntityTypeBuilder<MasterDataTable> builder)
    {
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
