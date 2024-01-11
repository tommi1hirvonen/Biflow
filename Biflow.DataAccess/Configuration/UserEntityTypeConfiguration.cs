using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class UserEntityTypeConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasMany(user => user.Subscriptions)
            .WithOne(subscription => subscription.User);

        builder.HasIndex(p => p.Username, "UQ_User")
            .IsUnique();

        builder.HasMany(u => u.Jobs)
            .WithMany(j => j.Users)
            .UsingEntity<Dictionary<string, object>>("JobAuthorization",
            x => x.HasOne<Job>().WithMany().HasForeignKey("JobId").OnDelete(DeleteBehavior.Cascade),
            x => x.HasOne<User>().WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade));

        builder.HasMany(u => u.DataTables)
            .WithMany(t => t.Users)
            .UsingEntity<Dictionary<string, object>>("DataTableAuthorization",
            x => x.HasOne<MasterDataTable>().WithMany().HasForeignKey("DataTableId").OnDelete(DeleteBehavior.Cascade),
            x => x.HasOne<User>().WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade));

        // Create shadow property to be used by Dapper/ADO.NET access in Ui.Core authentication.
        builder.Property<string?>("PasswordHash")
            .HasColumnName("PasswordHash")
            .HasColumnType("varchar(100)");
    }
}
