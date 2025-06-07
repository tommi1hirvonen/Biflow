namespace Biflow.DataAccess.Configuration;

public class FabricStepParameterEntityTypeConfiguration : IEntityTypeConfiguration<FabricStepParameter>
{
    public void Configure(EntityTypeBuilder<FabricStepParameter> builder)
    {
        builder.HasOne(p => p.Step)
            .WithMany(p => p.StepParameters)
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}