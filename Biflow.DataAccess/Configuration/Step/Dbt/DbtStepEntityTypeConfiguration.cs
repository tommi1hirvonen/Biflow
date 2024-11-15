namespace Biflow.DataAccess.Configuration;

internal class DbtStepEntityTypeConfiguration : IEntityTypeConfiguration<DbtStep>
{
    public void Configure(EntityTypeBuilder<DbtStep> builder)
    {
        builder.Property(x => x.TimeoutMinutes).HasColumnName("TimeoutMinutes");
    }
}
