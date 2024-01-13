namespace Biflow.DataAccess.Configuration;

internal class SqlStepEntityTypeConfiguration : IEntityTypeConfiguration<SqlStep>
{
    public void Configure(EntityTypeBuilder<SqlStep> builder)
    {
        builder.Property(x => x.TimeoutMinutes).HasColumnName("TimeoutMinutes");
        builder.Property(x => x.ConnectionId).HasColumnName("ConnectionId");

        builder.HasOne(x => x.ResultCaptureJobParameter)
            .WithMany(x => x.CapturingSteps)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
