﻿namespace Biflow.DataAccess.Configuration;

internal class FunctionStepExecutionParameterEntityTypeConfiguration : IEntityTypeConfiguration<FunctionStepExecutionParameter>
{
    public void Configure(EntityTypeBuilder<FunctionStepExecutionParameter> builder)
    {
        builder.HasOne(p => p.StepExecution)
            .WithMany(p => p.StepExecutionParameters)
            .HasForeignKey("ExecutionId", "StepId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
