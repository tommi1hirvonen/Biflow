using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class ExecutionDependencyEntityTypeConfiguration : IEntityTypeConfiguration<ExecutionDependency>
{
    public void Configure(EntityTypeBuilder<ExecutionDependency> builder)
    {
        builder.HasOne(d => d.StepExecution)
            .WithMany(e => e.ExecutionDependencies)
            .HasForeignKey(d => new { d.ExecutionId, d.StepId })
            .OnDelete(DeleteBehavior.ClientCascade);

        builder.HasOne(d => d.DependantOnStepExecution)
            .WithMany(e => e.DependantExecutions)
            .HasForeignKey(d => new { d.ExecutionId, d.DependantOnStepId })
            .OnDelete(DeleteBehavior.ClientCascade);

        builder.ToTable(t => t.HasCheckConstraint("CK_ExecutionDependency",
            $"[{nameof(ExecutionDependency.StepId)}]<>[{nameof(ExecutionDependency.DependantOnStepId)}]"));
    }
}
