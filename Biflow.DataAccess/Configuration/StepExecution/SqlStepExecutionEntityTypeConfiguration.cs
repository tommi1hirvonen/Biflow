using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class SqlStepExecutionEntityTypeConfiguration : IEntityTypeConfiguration<SqlStepExecution>
{
    public void Configure(EntityTypeBuilder<SqlStepExecution> builder)
    {
        builder.HasOne(x => x.ResultCaptureJobParameter)
            .WithMany(x => x.CapturingStepExecutions)
            .HasForeignKey(x => new { x.ExecutionId, x.ResultCaptureJobParameterId })
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}
