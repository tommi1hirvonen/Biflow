namespace Biflow.DataAccess.Configuration;

internal class DbNotebookStepEntityTypeConfiguration : IEntityTypeConfiguration<DbNotebookStep>
{
    public void Configure(EntityTypeBuilder<DbNotebookStep> builder)
    {
        builder.Property(x => x.TimeoutMinutes).HasColumnName("TimeoutMinutes");
    }
}
