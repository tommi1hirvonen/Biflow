namespace Biflow.DataAccess.Configuration;

internal class QlikStepExecutionEntityTypeConfiguration : IEntityTypeConfiguration<QlikStepExecution>
{
    public void Configure(EntityTypeBuilder<QlikStepExecution> builder)
    {
        builder.Property(x => x.AppId).IsUnicode(false);
    }
}
