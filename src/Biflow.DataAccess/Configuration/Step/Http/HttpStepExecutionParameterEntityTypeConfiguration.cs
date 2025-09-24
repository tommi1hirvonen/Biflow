namespace Biflow.DataAccess.Configuration;

internal class HttpStepExecutionParameterEntityTypeConfiguration : IEntityTypeConfiguration<HttpStepExecutionParameter>
{
    public void Configure(EntityTypeBuilder<HttpStepExecutionParameter> builder)
    {
        builder.HasOne(p => p.StepExecution)
            .WithMany(p => p.StepExecutionParameters)
            .HasForeignKey("ExecutionId", "StepId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
