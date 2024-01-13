namespace Biflow.DataAccess.Configuration;

internal class ExeStepParameterEntityTypeConfiguration : IEntityTypeConfiguration<ExeStepParameter>
{
    public void Configure(EntityTypeBuilder<ExeStepParameter> builder)
    {
        builder.HasOne(p => p.Step)
            .WithMany(p => p.StepParameters)
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}
