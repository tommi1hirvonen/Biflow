using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Biflow.DataAccess.Configuration;

internal class StepExecutionAttemptEntityTypeConfiguration : IEntityTypeConfiguration<StepExecutionAttempt>
{
    private static readonly JsonSerializerOptions IgnoreNullsOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public void Configure(EntityTypeBuilder<StepExecutionAttempt> builder)
    {
        builder.ToTable("ExecutionStepAttempt")
            .HasKey(x => new { x.ExecutionId, x.StepId, x.RetryAttemptIndex });

        builder.Ignore(x => x.UniqueId);
        builder.Ignore(x => x.ExecutionInSeconds);
        builder.Ignore(x => x.CanBeStopped);

        builder.HasDiscriminator<StepType>("StepType")
            .HasValue<DatasetStepExecutionAttempt>(StepType.Dataset)
            .HasValue<ExeStepExecutionAttempt>(StepType.Exe)
            .HasValue<JobStepExecutionAttempt>(StepType.Job)
            .HasValue<PackageStepExecutionAttempt>(StepType.Package)
            .HasValue<PipelineStepExecutionAttempt>(StepType.Pipeline)
            .HasValue<SqlStepExecutionAttempt>(StepType.Sql)
            .HasValue<FunctionStepExecutionAttempt>(StepType.Function)
            .HasValue<AgentJobStepExecutionAttempt>(StepType.AgentJob)
            .HasValue<TabularStepExecutionAttempt>(StepType.Tabular)
            .HasValue<EmailStepExecutionAttempt>(StepType.Email)
            .HasValue<QlikStepExecutionAttempt>(StepType.Qlik)
            .HasValue<DatabricksStepExecutionAttempt>(StepType.Databricks)
            .HasValue<DbtStepExecutionAttempt>(StepType.Dbt)
            .HasValue<ScdStepExecutionAttempt>(StepType.Scd)
            .HasValue<DataflowStepExecutionAttempt>(StepType.Dataflow);

        builder.Property(x => x.InfoMessages)
            .HasColumnName("InfoMessages")
            .HasConversion(
            from => JsonSerializer.Serialize(from, IgnoreNullsOptions),
            to => JsonSerializer.Deserialize<List<InfoMessage>>(to, IgnoreNullsOptions) ?? new(),
            new ValueComparer<IList<InfoMessage>>(
                (x, y) => x != null && y != null && x.SequenceEqual(y),
                x => x.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                x => x.ToList()));

        builder.Property(x => x.WarningMessages)
            .HasColumnName("WarningMessages")
            .HasConversion(
            from => JsonSerializer.Serialize(from, IgnoreNullsOptions),
            to => JsonSerializer.Deserialize<List<WarningMessage>>(to, IgnoreNullsOptions) ?? new(),
            new ValueComparer<IList<WarningMessage>>(
                (x, y) => x != null && y != null && x.SequenceEqual(y),
                x => x.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                x => x.ToList()));

        builder.Property(x => x.ErrorMessages)
            .HasColumnName("ErrorMessages")
            .HasConversion(
            from => JsonSerializer.Serialize(from, IgnoreNullsOptions),
            to => JsonSerializer.Deserialize<List<ErrorMessage>>(to, IgnoreNullsOptions) ?? new(),
            new ValueComparer<IList<ErrorMessage>>(
                (x, y) => x != null && y != null && x.SequenceEqual(y),
                x => x.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                x => x.ToList()));

        builder.HasOne(x => x.StepExecution)
            .WithMany(x => x.StepExecutionAttempts)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ExecutionStatus, x.EndedOn });
        builder.HasIndex(x => new { x.EndedOn });
    }
}
