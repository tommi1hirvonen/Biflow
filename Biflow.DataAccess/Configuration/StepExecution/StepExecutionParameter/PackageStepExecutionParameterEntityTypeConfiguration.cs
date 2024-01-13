namespace Biflow.DataAccess.Configuration;

internal class PackageStepExecutionParameterEntityTypeConfiguration : IEntityTypeConfiguration<PackageStepExecutionParameter>
{
    public void Configure(EntityTypeBuilder<PackageStepExecutionParameter> builder)
    {
        builder.HasOne(p => p.StepExecution)
            .WithMany(p => p.StepExecutionParameters)
            .HasForeignKey("ExecutionId", "StepId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
