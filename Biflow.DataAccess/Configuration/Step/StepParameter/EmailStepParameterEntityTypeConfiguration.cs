namespace Biflow.DataAccess.Configuration;

internal class EmailStepParameterEntityTypeConfiguration : IEntityTypeConfiguration<EmailStepParameter>
{
    public void Configure(EntityTypeBuilder<EmailStepParameter> builder)
    {
        builder.HasOne(p => p.Step)
            .WithMany(p => p.StepParameters)
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}
