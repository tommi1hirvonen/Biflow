namespace Biflow.DataAccess.Configuration;

internal class DbNotebookStepExecutionParameterEntityTypeConfiguration : IEntityTypeConfiguration<DbNotebookStepExecutionParameter>
{
    public void Configure(EntityTypeBuilder<DbNotebookStepExecutionParameter> builder)
    {
        builder.HasOne(p => p.StepExecution)
            .WithMany(p => p.StepExecutionParameters)
            .HasForeignKey("ExecutionId", "StepId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
