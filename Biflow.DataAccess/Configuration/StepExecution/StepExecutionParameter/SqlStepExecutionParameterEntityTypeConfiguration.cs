using Biflow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class SqlStepExecutionParameterEntityTypeConfiguration : IEntityTypeConfiguration<SqlStepExecutionParameter>
{
    public void Configure(EntityTypeBuilder<SqlStepExecutionParameter> builder)
    {
        builder.HasOne(p => p.StepExecution)
            .WithMany(p => p.StepExecutionParameters)
            .HasForeignKey("ExecutionId", "StepId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}