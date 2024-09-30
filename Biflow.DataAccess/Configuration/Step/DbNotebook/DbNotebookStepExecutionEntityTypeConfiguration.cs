namespace Biflow.DataAccess.Configuration;

internal class DbNotebookStepExecutionEntityTypeConfiguration : IEntityTypeConfiguration<DbNotebookStepExecution>
{
    public void Configure(EntityTypeBuilder<DbNotebookStepExecution> builder)
    {
        builder.Property(x => x.TimeoutMinutes).HasColumnName("TimeoutMinutes");
    }
}
