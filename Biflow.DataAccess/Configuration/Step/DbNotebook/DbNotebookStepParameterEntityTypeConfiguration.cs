namespace Biflow.DataAccess.Configuration;

internal class DbNotebookStepParameterEntityTypeConfiguration : IEntityTypeConfiguration<DbNotebookStepParameter>
{
    public void Configure(EntityTypeBuilder<DbNotebookStepParameter> builder)
    {
        builder.HasOne(p => p.Step)
            .WithMany(p => p.StepParameters)
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}
