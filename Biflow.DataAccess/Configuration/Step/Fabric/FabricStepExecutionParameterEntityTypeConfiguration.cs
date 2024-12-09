namespace Biflow.DataAccess.Configuration;

public class FabricStepExecutionParameterEntityTypeConfiguration : IEntityTypeConfiguration<FabricStepExecutionParameter>
{
    public void Configure(EntityTypeBuilder<FabricStepExecutionParameter> builder)
    {
        builder.HasOne(p => p.StepExecution)
            .WithMany(p => p.StepExecutionParameters)
            .HasForeignKey("ExecutionId", "StepId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}