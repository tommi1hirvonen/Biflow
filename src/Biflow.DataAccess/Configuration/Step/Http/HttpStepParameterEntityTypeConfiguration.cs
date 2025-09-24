namespace Biflow.DataAccess.Configuration;

internal class HttpStepParameterEntityTypeConfiguration : IEntityTypeConfiguration<HttpStepParameter>
{
    public void Configure(EntityTypeBuilder<HttpStepParameter> builder)
    {
        builder.HasOne(p => p.Step)
            .WithMany(p => p.StepParameters)
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}
