namespace Biflow.DataAccess.Configuration;

internal class SqlStepParameterEntityTypeConfiguration : IEntityTypeConfiguration<SqlStepParameter>
{
    public void Configure(EntityTypeBuilder<SqlStepParameter> builder)
    {
        builder.HasOne(p => p.Step)
            .WithMany(p => p.StepParameters)
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}
