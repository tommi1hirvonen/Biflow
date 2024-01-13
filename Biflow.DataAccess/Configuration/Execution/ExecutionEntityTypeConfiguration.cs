using Biflow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Biflow.DataAccess.Configuration;

internal class ExecutionEntityTypeConfiguration(AppDbContext context) : IEntityTypeConfiguration<Execution>
{
    public void Configure(EntityTypeBuilder<Execution> builder)
    {
        builder.ToTable("Execution")
            .HasKey(x => x.ExecutionId);

        builder.Property(x => x.CronExpression).IsUnicode(false);

        // The user is either admin or editor or is granted authorization to the job.
        builder.HasQueryFilter(exec =>
            context.UserRoles == null ||
            context.UserRoles.Contains(Roles.Admin) ||
            context.UserRoles.Contains(Roles.Editor) ||
            context.Users.Any(u => u.Username == context.Username && (u.AuthorizeAllJobs || u.Jobs.Any(j => j.JobId == exec.JobId))));

        builder.Property(p => p.ParentExecution).HasConversion(
            from => JsonSerializer.Serialize(from, null as JsonSerializerOptions),
            to => JsonSerializer.Deserialize<StepExecutionAttemptReference?>(to, null as JsonSerializerOptions));

        builder.Property(p => p.ParentExecution)
            .IsUnicode(false);

        builder.HasIndex(x => new { x.CreatedOn, x.EndedOn }, "IX_Execution_CreatedOn_EndedOn");
        builder.HasIndex(x => x.ExecutionStatus, "IX_Execution_ExecutionStatus");
        builder.HasIndex(x => new { x.JobId, x.CreatedOn }, "IX_Execution_JobId_CreatedOn");
    }
}
