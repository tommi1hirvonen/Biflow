namespace Biflow.DataAccess.Configuration;

internal class TabularStepEntityTypeConfiguration : IEntityTypeConfiguration<TabularStep>
{
    public void Configure(EntityTypeBuilder<TabularStep> builder)
    {
        builder.Property(x => x.TimeoutMinutes).HasColumnName("TimeoutMinutes");
        builder.Property(x => x.ConnectionId).HasColumnName("ConnectionId");
        builder.HasOne(x => x.Connection).WithMany(x => x.TabularSteps);
    }
}
