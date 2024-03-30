using System.Text.Json;

namespace Biflow.DataAccess.Configuration;

internal class ExecutionConditionParameterEntityTypeConfiguration : IEntityTypeConfiguration<ExecutionConditionParameter>
{
    public void Configure(EntityTypeBuilder<ExecutionConditionParameter> builder)
    {
        builder.ToTable("StepConditionParameter")
            .HasKey(x => x.ParameterId);

        builder.Property(p => p.ParameterValue).HasConversion(
            from => JsonSerializer.Serialize(from, null as JsonSerializerOptions),
            to => JsonSerializer.Deserialize<ParameterValue?>(to, null as JsonSerializerOptions) ?? new());

        builder.HasOne(x => x.Step)
            .WithMany(x => x.ExecutionConditionParameters)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.JobParameter)
            .WithMany(x => x.ExecutionConditionParameters)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(x => new { x.StepId, x.ParameterName }, "UQ_StepConditionParameter")
            .IsUnique();
    }
}
