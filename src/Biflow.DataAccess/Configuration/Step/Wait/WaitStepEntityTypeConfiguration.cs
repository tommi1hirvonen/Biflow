namespace Biflow.DataAccess.Configuration;

internal class WaitStepEntityTypeConfiguration : IEntityTypeConfiguration<WaitStep>
{
    public void Configure(EntityTypeBuilder<WaitStep> builder)
    {
        builder.Property(x => x.WaitSeconds).HasColumnName("WaitSeconds");
    }
}