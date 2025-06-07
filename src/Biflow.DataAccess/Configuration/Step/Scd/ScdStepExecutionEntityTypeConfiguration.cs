namespace Biflow.DataAccess.Configuration.Scd;

public class ScdStepExecutionEntityTypeConfiguration : IEntityTypeConfiguration<ScdStepExecution>
{
    public void Configure(EntityTypeBuilder<ScdStepExecution> builder)
    {
        builder.Property(x => x.TimeoutMinutes).HasColumnName("TimeoutMinutes");
        builder.Property(x => x.ScdTableId).HasColumnName("ScdTableId");
    }
}