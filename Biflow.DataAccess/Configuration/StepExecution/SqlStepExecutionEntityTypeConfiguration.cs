namespace Biflow.DataAccess.Configuration;

internal class SqlStepExecutionEntityTypeConfiguration : IEntityTypeConfiguration<SqlStepExecution>
{
    public void Configure(EntityTypeBuilder<SqlStepExecution> builder)
    {
        builder.Property(x => x.TimeoutMinutes).HasColumnName("TimeoutMinutes");
        builder.Property(x => x.ConnectionId).HasColumnName("ConnectionId");
        builder.Property(x => x.ResultCaptureJobParameterValue).HasColumnType("sql_variant");

        builder.HasOne(x => x.ResultCaptureJobParameter)
            .WithMany(x => x.CapturingStepExecutions)
            .HasForeignKey(x => new { x.ExecutionId, x.ResultCaptureJobParameterId })
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}
