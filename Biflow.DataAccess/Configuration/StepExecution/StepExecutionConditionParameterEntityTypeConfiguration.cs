using Biflow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class StepExecutionConditionParameterEntityTypeConfiguration : IEntityTypeConfiguration<StepExecutionConditionParameter>
{
    public void Configure(EntityTypeBuilder<StepExecutionConditionParameter> builder)
    {
        builder.ToTable("ExecutionStepConditionParameter")
            .HasKey(x => new { x.ExecutionId, x.ParameterId });

        builder.Property(x => x.ExecutionId)
            .HasColumnName("ExecutionId");

        builder.Property(x => x.StepId)
            .HasColumnName("StepId");

        builder.Property(x => x.ParameterValue)
            .HasColumnType("sql_variant");

        builder.Property(x => x.ExecutionParameterValue)
            .HasColumnType("sql_variant");

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
