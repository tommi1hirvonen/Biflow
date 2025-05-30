﻿using System.Text.Json;

namespace Biflow.DataAccess.Configuration;

internal class StepExecutionParameterEntityTypeConfiguration : IEntityTypeConfiguration<StepExecutionParameterBase>
{
    public void Configure(EntityTypeBuilder<StepExecutionParameterBase> builder)
    {
        builder.ToTable("ExecutionStepParameter")
            .HasKey(x => new { x.ExecutionId, x.ParameterId });

        builder.Property(p => p.ParameterValue).HasConversion(
            from => JsonSerializer.Serialize(from, null as JsonSerializerOptions),
            to => JsonSerializer.Deserialize<ParameterValue?>(to, null as JsonSerializerOptions) ?? new());

        builder.Property(p => p.ExecutionParameterValue).HasConversion(
            from => JsonSerializer.Serialize(from, null as JsonSerializerOptions),
            to => JsonSerializer.Deserialize<ParameterValue?>(to, null as JsonSerializerOptions) ?? new());

        builder.Ignore("Evaluated");
        builder.Ignore("EvaluationResult");
        builder.Ignore(x => x.JobParameters);

        builder.HasOne(p => p.InheritFromExecutionParameter)
            .WithMany(p => p.StepExecutionParameters)
            .HasForeignKey(p => new { p.ExecutionId, p.InheritFromExecutionParameterId })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasDiscriminator<ParameterType>("ParameterType")
            .HasValue<SqlStepExecutionParameter>(ParameterType.Sql)
            .HasValue<PackageStepExecutionParameter>(ParameterType.Package)
            .HasValue<JobStepExecutionParameter>(ParameterType.Job)
            .HasValue<ExeStepExecutionParameter>(ParameterType.Exe)
            .HasValue<FunctionStepExecutionParameter>(ParameterType.Function)
            .HasValue<PipelineStepExecutionParameter>(ParameterType.Pipeline)
            .HasValue<EmailStepExecutionParameter>(ParameterType.Email)
            .HasValue<DatabricksStepExecutionParameter>(ParameterType.DatabricksNotebook)
            .HasValue<FabricStepExecutionParameter>(ParameterType.Fabric);

        builder.OwnsOne(s => s.Expression, ece =>
        {
            ece.Property(p => p.Expression).HasColumnName("Expression");
        });
    }
}
