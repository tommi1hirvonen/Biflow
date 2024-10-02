namespace Biflow.DataAccess.Configuration;

internal class DatabricksStepExecutionParameterEntityTypeConfiguration : IEntityTypeConfiguration<DatabricksStepExecutionParameter>
{
    public void Configure(EntityTypeBuilder<DatabricksStepExecutionParameter> builder)
    {
        builder.HasOne(p => p.StepExecution)
            .WithMany(p => p.StepExecutionParameters)
            .HasForeignKey("ExecutionId", "StepId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
