namespace Biflow.DataAccess.Configuration;

internal class StepExecutionMonitorEntityTypeConfiguration : IEntityTypeConfiguration<StepExecutionMonitor>
{
    public void Configure(EntityTypeBuilder<StepExecutionMonitor> builder)
    {
        builder.ToTable("ExecutionStepMonitor")
            .HasKey(e => new { e.ExecutionId, e.StepId, e.MonitoredExecutionId, e.MonitoredStepId, e.MonitoringReason });

        builder.HasOne(e => e.StepExecution)
            .WithMany(e => e.MonitoredStepExecutions)
            .HasForeignKey(e => new { e.ExecutionId, e.StepId })
            .OnDelete(DeleteBehavior.ClientCascade);
        builder.HasOne(e => e.MonitoredStepExecution)
            .WithMany(e => e.MonitoringStepExecutions)
            .HasForeignKey(e => new { e.MonitoredExecutionId, e.MonitoredStepId })
            .OnDelete(DeleteBehavior.ClientCascade);

        builder.HasIndex(e => new { e.MonitoredExecutionId, e.MonitoredStepId });
    }
}
