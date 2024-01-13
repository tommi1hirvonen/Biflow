namespace Biflow.DataAccess.Configuration;

internal class FunctionStepExecutionEntityTypeConfiguration : IEntityTypeConfiguration<FunctionStepExecution>
{
    public void Configure(EntityTypeBuilder<FunctionStepExecution> builder)
    {
        builder.Property(x => x.FunctionUrl).IsUnicode(false);
    }
}
