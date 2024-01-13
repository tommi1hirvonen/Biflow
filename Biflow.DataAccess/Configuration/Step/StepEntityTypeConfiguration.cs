using Biflow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class StepEntityTypeConfiguration : IEntityTypeConfiguration<Step>
{
    public void Configure(EntityTypeBuilder<Step> builder)
    {
        builder.HasDiscriminator<StepType>("StepType")
            .HasValue<DatasetStep>(StepType.Dataset)
            .HasValue<ExeStep>(StepType.Exe)
            .HasValue<JobStep>(StepType.Job)
            .HasValue<PackageStep>(StepType.Package)
            .HasValue<PipelineStep>(StepType.Pipeline)
            .HasValue<SqlStep>(StepType.Sql)
            .HasValue<FunctionStep>(StepType.Function)
            .HasValue<AgentJobStep>(StepType.AgentJob)
            .HasValue<TabularStep>(StepType.Tabular)
            .HasValue<EmailStep>(StepType.Email)
            .HasValue<QlikStep>(StepType.Qlik);

        builder.OwnsOne(s => s.ExecutionConditionExpression, ece =>
        {
            ece.Property(p => p.Expression).HasColumnName("ExecutionConditionExpression");
        });

        builder.HasOne(x => x.Job)
            .WithMany(x => x.Steps)
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}
