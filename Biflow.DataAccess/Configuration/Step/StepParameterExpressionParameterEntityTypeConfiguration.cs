﻿namespace Biflow.DataAccess.Configuration;

internal class StepParameterExpressionParameterEntityTypeConfiguration : IEntityTypeConfiguration<StepParameterExpressionParameter>
{
    public void Configure(EntityTypeBuilder<StepParameterExpressionParameter> builder)
    {
        builder.ToTable("StepParameterExpressionParameter")
            .HasKey(x => x.ParameterId);

        builder.HasOne(x => x.InheritFromJobParameter)
            .WithMany(x => x.InheritingStepParameterExpressionParameters)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.StepParameter)
            .WithMany(x => x.ExpressionParameters)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.StepParameterId, x.ParameterName }, "UQ_StepParameterExpressionParameter")
            .IsUnique();
    }
}
