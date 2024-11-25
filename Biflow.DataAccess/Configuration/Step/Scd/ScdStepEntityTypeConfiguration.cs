namespace Biflow.DataAccess.Configuration.Scd;

public class ScdStepEntityTypeConfiguration : IEntityTypeConfiguration<ScdStep>
{
    public void Configure(EntityTypeBuilder<ScdStep> builder)
    {
        builder.Property(x => x.TimeoutMinutes).HasColumnName("TimeoutMinutes");
        builder.Property(x => x.ScdTableId).HasColumnName("ScdTableId");
        builder.HasOne(x => x.ScdTable).WithMany(x => x.ScdSteps);
        builder.Property(x => x.ScdTableId).IsRequired();
    }
}