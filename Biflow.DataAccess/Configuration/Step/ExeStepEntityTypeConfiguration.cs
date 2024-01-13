namespace Biflow.DataAccess.Configuration;

internal class ExeStepEntityTypeConfiguration : IEntityTypeConfiguration<ExeStep>
{
    public void Configure(EntityTypeBuilder<ExeStep> builder)
    {
        builder.Property(x => x.TimeoutMinutes).HasColumnName("TimeoutMinutes");
    }
}
