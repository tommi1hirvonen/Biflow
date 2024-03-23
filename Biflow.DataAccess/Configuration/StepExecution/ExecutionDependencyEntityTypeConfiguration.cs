namespace Biflow.DataAccess.Configuration;

internal class ExecutionDependencyEntityTypeConfiguration : IEntityTypeConfiguration<ExecutionDependency>
{
    public void Configure(EntityTypeBuilder<ExecutionDependency> builder)
    {
        builder.ToTable("ExecutionDependency")
            .HasKey(x => new { x.ExecutionId, x.StepId, x.DependantOnStepId });

        builder.HasOne(d => d.StepExecution)
            .WithMany(e => e.ExecutionDependencies)
            .HasForeignKey(d => new { d.ExecutionId, d.StepId })
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.DependantOnStepId)
            .HasColumnName("DependantOnStepId");

        builder.ToTable(t => t.HasCheckConstraint("CK_ExecutionDependency",
            $"[{nameof(ExecutionDependency.StepId)}]<>[{nameof(ExecutionDependency.DependantOnStepId)}]"));
    }
}
