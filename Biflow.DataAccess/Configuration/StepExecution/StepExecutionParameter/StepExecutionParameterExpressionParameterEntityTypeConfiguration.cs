using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class StepExecutionParameterExpressionParameterEntityTypeConfiguration
    : IEntityTypeConfiguration<StepExecutionParameterExpressionParameter>
{
    public void Configure(EntityTypeBuilder<StepExecutionParameterExpressionParameter> builder)
    {
        builder.HasOne(x => x.StepParameter)
            .WithMany(x => x.ExpressionParameters)
            .HasForeignKey("ExecutionId", "StepParameterId")
            .OnDelete(DeleteBehavior.ClientCascade);

        builder.HasOne(x => x.InheritFromExecutionParameter)
            .WithMany(x => x.StepExecutionParameterExpressionParameters)
            .HasForeignKey("ExecutionId", "InheritFromExecutionParameterId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ExecutionId, x.StepParameterId, x.ParameterName }, "UQ_ExecutionStepParameterExpressionParameter")
            .IsUnique();
    }
}
