using System.Text.Json;

namespace Biflow.DataAccess.Configuration;

internal class SqlStepExecutionEntityTypeConfiguration : IEntityTypeConfiguration<SqlStepExecution>
{
    public void Configure(EntityTypeBuilder<SqlStepExecution> builder)
    {
        builder.Property(x => x.TimeoutMinutes).HasColumnName("TimeoutMinutes");
        builder.Property(x => x.ConnectionId).HasColumnName("ConnectionId");

        builder.Property(p => p.ResultCaptureJobParameterValue).HasConversion(
            from => JsonSerializer.Serialize(from, null as JsonSerializerOptions),
            to => JsonSerializer.Deserialize<ParameterValue?>(to, null as JsonSerializerOptions) ?? new());

        builder.HasOne(x => x.ResultCaptureJobParameter)
            .WithMany(x => x.CapturingStepExecutions)
            .HasForeignKey(x => new { x.ExecutionId, x.ResultCaptureJobParameterId })
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}
