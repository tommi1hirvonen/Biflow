using Biflow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class StepExecutionEntityTypeConfiguration : IEntityTypeConfiguration<StepExecution>
{
    public void Configure(EntityTypeBuilder<StepExecution> builder)
    {
        builder.ToTable("ExecutionStep")
            .HasKey(x => new { x.ExecutionId, x.StepId });

        builder.HasDiscriminator<StepType>("StepType")
            .HasValue<DatasetStepExecution>(StepType.Dataset)
            .HasValue<ExeStepExecution>(StepType.Exe)
            .HasValue<JobStepExecution>(StepType.Job)
            .HasValue<PackageStepExecution>(StepType.Package)
            .HasValue<PipelineStepExecution>(StepType.Pipeline)
            .HasValue<SqlStepExecution>(StepType.Sql)
            .HasValue<FunctionStepExecution>(StepType.Function)
            .HasValue<AgentJobStepExecution>(StepType.AgentJob)
            .HasValue<TabularStepExecution>(StepType.Tabular)
            .HasValue<EmailStepExecution>(StepType.Email)
            .HasValue<QlikStepExecution>(StepType.Qlik);

        builder.OwnsOne(s => s.ExecutionConditionExpression, ece =>
        {
            ece.Property(p => p.Expression).HasColumnName("ExecutionConditionExpression");
        });
        builder.HasOne(x => x.Execution)
            .WithMany(x => x.StepExecutions)
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}
