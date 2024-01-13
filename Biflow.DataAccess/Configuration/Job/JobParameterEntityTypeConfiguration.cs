namespace Biflow.DataAccess.Configuration;

internal class JobParameterEntityTypeConfiguration : IEntityTypeConfiguration<JobParameter>
{
    public void Configure(EntityTypeBuilder<JobParameter> builder)
    {
        builder.ToTable("JobParameter")
            .HasKey(x => x.ParameterId);

        builder.Property(x => x.JobId)
            .HasColumnName("JobId");

        builder.OwnsOne(s => s.Expression, ece =>
        {
            ece.Property(p => p.Expression).HasColumnName("Expression");
        });
        builder.HasIndex(x => new { x.JobId, x.ParameterName }, "UQ_JobParameter")
            .IsUnique();
        builder.HasOne(x => x.Job)
            .WithMany(x => x.JobParameters)
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}
