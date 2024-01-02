using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class ExecutionConditionParameterEntityTypeConfiguration : IEntityTypeConfiguration<ExecutionConditionParameter>
{
    public void Configure(EntityTypeBuilder<ExecutionConditionParameter> builder)
    {
        builder.HasOne(x => x.Step)
            .WithMany(x => x.ExecutionConditionParameters)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.JobParameter)
            .WithMany(x => x.ExecutionConditionParameters)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(x => new { x.StepId, x.ParameterName }, "UQ_StepConditionParameter")
            .IsUnique();
    }
}
