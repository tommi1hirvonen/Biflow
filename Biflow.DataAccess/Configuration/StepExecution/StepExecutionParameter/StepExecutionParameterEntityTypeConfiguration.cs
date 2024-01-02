using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class StepExecutionParameterEntityTypeConfiguration : IEntityTypeConfiguration<StepExecutionParameterBase>
{
    public void Configure(EntityTypeBuilder<StepExecutionParameterBase> builder)
    {
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
            .HasValue<EmailStepExecutionParameter>(ParameterType.Email);

        builder.OwnsOne(s => s.Expression, ece =>
        {
            ece.Property(p => p.Expression).HasColumnName("Expression");
        });
    }
}
