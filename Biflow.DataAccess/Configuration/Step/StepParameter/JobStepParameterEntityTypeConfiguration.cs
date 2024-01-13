namespace Biflow.DataAccess.Configuration;

internal class JobStepParameterEntityTypeConfiguration : IEntityTypeConfiguration<JobStepParameter>
{
    public void Configure(EntityTypeBuilder<JobStepParameter> builder)
    {
        builder.HasOne(p => p.Step)
            .WithMany(p => p.StepParameters)
            .OnDelete(DeleteBehavior.ClientCascade);

        builder.HasOne(p => p.AssignToJobParameter)
            .WithMany(p => p.AssigningStepParameters)
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}
