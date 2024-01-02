using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class StepExecutionConditionParameterEntityTypeConfiguration : IEntityTypeConfiguration<StepExecutionConditionParameter>
{
    public void Configure(EntityTypeBuilder<StepExecutionConditionParameter> builder)
    {
        builder.HasOne(p => p.StepExecution)
            .WithMany(e => e.ExecutionConditionParameters)
            .HasForeignKey("ExecutionId", "StepId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.ExecutionParameter)
            .WithMany(e => e.ExecutionConditionParameters)
            .HasForeignKey("ExecutionId", "ExecutionParameterId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
