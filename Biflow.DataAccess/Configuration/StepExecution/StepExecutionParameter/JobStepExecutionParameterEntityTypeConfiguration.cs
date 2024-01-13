namespace Biflow.DataAccess.Configuration;

internal class JobStepExecutionParameterEntityTypeConfiguration : IEntityTypeConfiguration<JobStepExecutionParameter>
{
    public void Configure(EntityTypeBuilder<JobStepExecutionParameter> builder)
    {
        builder.HasOne(p => p.StepExecution)
            .WithMany(p => p.StepExecutionParameters)
            .HasForeignKey("ExecutionId", "StepId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}