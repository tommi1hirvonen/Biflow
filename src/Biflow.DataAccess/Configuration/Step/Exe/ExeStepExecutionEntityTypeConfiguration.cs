namespace Biflow.DataAccess.Configuration;

internal class ExeStepExecutionEntityTypeConfiguration : IEntityTypeConfiguration<ExeStepExecution>
{
    public void Configure(EntityTypeBuilder<ExeStepExecution> builder)
    {
        builder.Property(x => x.TimeoutMinutes).HasColumnName("TimeoutMinutes");
        builder.Property(x => x.RunAsCredentialId).HasColumnName("ExeRunAsCredentialId");
        builder.Property(x => x.RunAsUsername)
            .HasColumnName("ExeRunAsUsername")
            .HasMaxLength(400);
    }
}
