namespace Biflow.DataAccess.Configuration;

internal class PipelineStepEntityTypeConfiguration : IEntityTypeConfiguration<PipelineStep>
{
    public void Configure(EntityTypeBuilder<PipelineStep> builder)
    {
        builder.Property(x => x.TimeoutMinutes).HasColumnName("TimeoutMinutes");
    }
}
