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
            .HasValue<QlikStepExecutionAttempt>(StepType.Qlik);

        builder.Ignore(x => x.InfoMessages);
        builder.Ignore(x => x.WarningMessages);
        builder.Ignore(x => x.ErrorMessages);

        builder.Property<List<InfoMessage>>("_infoMessages")
            .HasColumnName("InfoMessages")
            .HasConversion(
            from => JsonSerializer.Serialize(from, IgnoreNullsOptions),
            to => JsonSerializer.Deserialize<List<InfoMessage>>(to, IgnoreNullsOptions) ?? new(),
            new ValueComparer<List<InfoMessage>>(
                (x, y) => x != null && y != null && x.SequenceEqual(y),
                x => x.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                x => x.ToList()));

        builder.Property<List<WarningMessage>>("_warningMessages")
            .HasColumnName("WarningMessages")
            .HasConversion(
            from => JsonSerializer.Serialize(from, IgnoreNullsOptions),
            to => JsonSerializer.Deserialize<List<WarningMessage>>(to, IgnoreNullsOptions) ?? new(),
            new ValueComparer<List<WarningMessage>>(
                (x, y) => x != null && y != null && x.SequenceEqual(y),
                x => x.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                x => x.ToList()));

        builder.Property<List<ErrorMessage>>("_errorMessages")
            .HasColumnName("ErrorMessages")
            .HasConversion(
            from => JsonSerializer.Serialize(from, IgnoreNullsOptions),
            to => JsonSerializer.Deserialize<List<ErrorMessage>>(to, IgnoreNullsOptions) ?? new(),
            new ValueComparer<List<ErrorMessage>>(
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
