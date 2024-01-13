namespace Biflow.DataAccess.Configuration;

internal class PipelineStepParameterEntityTypeConfiguration : IEntityTypeConfiguration<PipelineStepParameter>
{
    public void Configure(EntityTypeBuilder<PipelineStepParameter> builder)
    {
        builder.HasOne(p => p.Step)
            .WithMany(p => p.StepParameters)
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}
