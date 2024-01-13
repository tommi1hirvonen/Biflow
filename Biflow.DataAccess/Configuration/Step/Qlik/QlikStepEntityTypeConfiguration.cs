namespace Biflow.DataAccess.Configuration;

internal class QlikStepEntityTypeConfiguration : IEntityTypeConfiguration<QlikStep>
{
    public void Configure(EntityTypeBuilder<QlikStep> builder)
    {
        builder.Property(x => x.AppId).IsUnicode(false);
        builder.Property(x => x.TimeoutMinutes).HasColumnName("TimeoutMinutes");
    }
}
