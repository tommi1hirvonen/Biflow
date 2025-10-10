namespace Biflow.DataAccess.Configuration;

internal class FunctionStepExecutionEntityTypeConfiguration : IEntityTypeConfiguration<FunctionStepExecution>
{
    public void Configure(EntityTypeBuilder<FunctionStepExecution> builder)
    {
        builder.Property(x => x.TimeoutMinutes).HasColumnName("TimeoutMinutes");
        builder.Property(x => x.DisableAsyncPattern).HasColumnName("FunctionDisableAsyncPattern");
        builder.Property(x => x.FunctionUrl).IsUnicode(false);
    }
}
