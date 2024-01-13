namespace Biflow.DataAccess.Configuration;

internal class DependencyEntityTypeConfiguration : IEntityTypeConfiguration<Dependency>
{
    public void Configure(EntityTypeBuilder<Dependency> builder)
    {
        builder.ToTable("Dependency")
            .HasKey(x => new { x.StepId, x.DependantOnStepId });

        builder.HasOne(dependency => dependency.Step)
            .WithMany(step => step.Dependencies)
            .OnDelete(DeleteBehavior.ClientCascade);

        builder.HasOne(dependency => dependency.DependantOnStep)
            .WithMany(step => step.Depending)
            .OnDelete(DeleteBehavior.ClientCascade);

        builder.ToTable(t => t.HasCheckConstraint("CK_Dependency",
            $"[{nameof(Dependency.StepId)}]<>[{nameof(Dependency.DependantOnStepId)}]"));
    }
}
