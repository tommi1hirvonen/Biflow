namespace Biflow.DataAccess.Configuration;

internal class DatabricksStepParameterEntityTypeConfiguration : IEntityTypeConfiguration<DatabricksStepParameter>
{
    public void Configure(EntityTypeBuilder<DatabricksStepParameter> builder)
    {
        builder.HasOne(p => p.Step)
            .WithMany(p => p.StepParameters)
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}
