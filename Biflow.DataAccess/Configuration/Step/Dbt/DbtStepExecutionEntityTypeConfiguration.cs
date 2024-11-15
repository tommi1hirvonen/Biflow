namespace Biflow.DataAccess.Configuration;

internal class DbtStepExecutionEntityTypeConfiguration : IEntityTypeConfiguration<DbtStepExecution>
{
    public void Configure(EntityTypeBuilder<DbtStepExecution> builder)
    {
        builder.Property(x => x.TimeoutMinutes).HasColumnName("TimeoutMinutes");
    }
}
