namespace Biflow.DataAccess.Configuration;

internal class DbtStepExecutionEntityTypeConfiguration : IEntityTypeConfiguration<DbtStepExecution>
{
    public void Configure(EntityTypeBuilder<DbtStepExecution> builder)
    {
        builder.Property(x => x.TimeoutMinutes).HasColumnName("TimeoutMinutes");
        builder.ComplexProperty(x => x.DbtJob, dbtj =>
        {
            dbtj.Property(x => x.Id).HasColumnName("DbtJobId").IsRequired();
            dbtj.Property(x => x.Name).HasColumnName("DbtJobName").HasMaxLength(500);
            dbtj.Property(x => x.EnvironmentId).HasColumnName("DbtJobEnvironmentId");
            dbtj.Property(x => x.EnvironmentName).HasColumnName("DbtJobEnvironmentName").HasMaxLength(500);
            dbtj.Property(x => x.ProjectId).HasColumnName("DbtJobProjectId");
            dbtj.Property(x => x.ProjectName).HasColumnName("DbtJobProjectName").HasMaxLength(500);
        });
    }
}
