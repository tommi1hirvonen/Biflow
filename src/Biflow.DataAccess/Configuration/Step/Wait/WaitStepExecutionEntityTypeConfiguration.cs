namespace Biflow.DataAccess.Configuration;

internal class WaitStepExecutionEntityTypeConfiguration : IEntityTypeConfiguration<WaitStepExecution>
{
    public void Configure(EntityTypeBuilder<WaitStepExecution> builder)
    {
        builder.Property(x => x.WaitSeconds).HasColumnName("WaitSeconds");
    }
}