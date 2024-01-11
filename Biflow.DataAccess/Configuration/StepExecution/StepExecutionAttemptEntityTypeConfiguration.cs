using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
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

        builder.Property(p => p.InfoMessages).HasConversion(
            from => JsonSerializer.Serialize(from, IgnoreNullsOptions),
            to => JsonSerializer.Deserialize<List<InfoMessage>>(to, IgnoreNullsOptions) ?? new(),
            new ValueComparer<List<InfoMessage>>(
                (x, y) => x != null && y != null && x.SequenceEqual(y),
                x => x.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                x => x.ToList()));

        builder.Property(p => p.WarningMessages).HasConversion(
            from => JsonSerializer.Serialize(from, IgnoreNullsOptions),
            to => JsonSerializer.Deserialize<List<WarningMessage>>(to, IgnoreNullsOptions) ?? new(),
            new ValueComparer<List<WarningMessage>>(
                (x, y) => x != null && y != null && x.SequenceEqual(y),
                x => x.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                x => x.ToList()));

        builder.Property(p => p.ErrorMessages).HasConversion(
            from => JsonSerializer.Serialize(from, IgnoreNullsOptions),
            to => JsonSerializer.Deserialize<List<ErrorMessage>>(to, IgnoreNullsOptions) ?? new(),
            new ValueComparer<List<ErrorMessage>>(
                (x, y) => x != null && y != null && x.SequenceEqual(y),
                x => x.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                x => x.ToList()));

        builder.HasOne(x => x.StepExecution)
            .WithMany(x => x.StepExecutionAttempts)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
