namespace Biflow.DataAccess.Configuration;

internal class ExeStepExecutionParameterEntityTypeConfiguration : IEntityTypeConfiguration<ExeStepExecutionParameter>
{
    public void Configure(EntityTypeBuilder<ExeStepExecutionParameter> builder)
    {
        builder.HasOne(p => p.StepExecution)
            .WithMany(p => p.StepExecutionParameters)
            .HasForeignKey("ExecutionId", "StepId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
