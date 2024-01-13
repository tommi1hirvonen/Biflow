namespace Biflow.DataAccess.Configuration;

internal class EmailStepExecutionParameterEntityTypeConfiguration : IEntityTypeConfiguration<EmailStepExecutionParameter>
{
    public void Configure(EntityTypeBuilder<EmailStepExecutionParameter> builder)
    {
        builder.HasOne(p => p.StepExecution)
            .WithMany(p => p.StepExecutionParameters)
            .HasForeignKey("ExecutionId", "StepId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
