namespace Biflow.DataAccess.Configuration;

internal class PipelineStepExecutionParameterEntityTypeConfiguration : IEntityTypeConfiguration<PipelineStepExecutionParameter>
{
    public void Configure(EntityTypeBuilder<PipelineStepExecutionParameter> builder)
    {
        builder.HasOne(p => p.StepExecution)
            .WithMany(p => p.StepExecutionParameters)
            .HasForeignKey("ExecutionId", "StepId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
