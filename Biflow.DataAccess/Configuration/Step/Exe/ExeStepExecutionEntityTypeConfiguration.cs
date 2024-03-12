namespace Biflow.DataAccess.Configuration;

internal class ExeStepExecutionEntityTypeConfiguration : IEntityTypeConfiguration<ExeStepExecution>
{
    public void Configure(EntityTypeBuilder<ExeStepExecution> builder)
    {
        builder.Property(x => x.TimeoutMinutes).HasColumnName("TimeoutMinutes");

        builder.Property(x => x.Domain)
            .HasColumnName("ExeDomain")
            .HasMaxLength(200);
        builder.Property(x => x.Username)
            .HasColumnName("ExeUsername")
            .HasMaxLength(200);
        builder.Property(x => x.Password)
            .HasColumnName("ExePassword")
            .HasMaxLength(200);
    }
}
