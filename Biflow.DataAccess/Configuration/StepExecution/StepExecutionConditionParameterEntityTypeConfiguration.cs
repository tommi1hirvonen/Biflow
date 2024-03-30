using System.Text.Json;

namespace Biflow.DataAccess.Configuration;

internal class StepExecutionConditionParameterEntityTypeConfiguration : IEntityTypeConfiguration<StepExecutionConditionParameter>
{
    public void Configure(EntityTypeBuilder<StepExecutionConditionParameter> builder)
    {
        builder.ToTable("ExecutionStepConditionParameter")
            .HasKey(x => new { x.ExecutionId, x.ParameterId });

        builder.Property(x => x.ExecutionId)
            .HasColumnName("ExecutionId");

        builder.Property(x => x.StepId)
            .HasColumnName("StepId");

        builder.Property(p => p.ParameterValue).HasConversion(
            from => JsonSerializer.Serialize(from, null as JsonSerializerOptions),
            to => JsonSerializer.Deserialize<ParameterValue?>(to, null as JsonSerializerOptions) ?? new());

        builder.Property(p => p.ExecutionParameterValue).HasConversion(
            from => JsonSerializer.Serialize(from, null as JsonSerializerOptions),
            to => JsonSerializer.Deserialize<ParameterValue?>(to, null as JsonSerializerOptions) ?? new());

        builder.HasOne(p => p.StepExecution)
            .WithMany(e => e.ExecutionConditionParameters)
            .HasForeignKey("ExecutionId", "StepId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.ExecutionParameter)
            .WithMany(e => e.ExecutionConditionParameters)
            .HasForeignKey("ExecutionId", "ExecutionParameterId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
