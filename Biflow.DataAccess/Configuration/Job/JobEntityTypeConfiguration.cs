using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class JobEntityTypeConfiguration(AppDbContext context) : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.HasOne(j => j.Category)
        .WithMany(c => c.Jobs)
        .OnDelete(DeleteBehavior.SetNull);

        // The user is either admin or editor or is granted authorization to the job.
        builder.HasQueryFilter(j =>
            context.UserRoles == null ||
            context.UserRoles.Contains(Roles.Admin) ||
            context.UserRoles.Contains(Roles.Editor) ||
            context.Users.Any(u => u.Username == context.Username && u.AuthorizeAllJobs) ||
            j.Users.Any(u => u.Username == context.Username));
    }
}
