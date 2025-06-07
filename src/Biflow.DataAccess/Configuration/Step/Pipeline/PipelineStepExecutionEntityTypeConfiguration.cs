namespace Biflow.DataAccess.Configuration;

internal class PipelineStepExecutionEntityTypeConfiguration : IEntityTypeConfiguration<PipelineStepExecution>
{
    public void Configure(EntityTypeBuilder<PipelineStepExecution> builder)
    {
        builder.Property(x => x.TimeoutMinutes).HasColumnName("TimeoutMinutes");
    }
}
