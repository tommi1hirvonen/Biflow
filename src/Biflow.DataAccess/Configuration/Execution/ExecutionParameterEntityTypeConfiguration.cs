using System.Text.Json;

namespace Biflow.DataAccess.Configuration;

internal class ExecutionParameterEntityTypeConfiguration : IEntityTypeConfiguration<ExecutionParameter>
{
    public void Configure(EntityTypeBuilder<ExecutionParameter> builder)
    {
        builder.ToTable("ExecutionParameter")
            .HasKey(x => new { x.ExecutionId, x.ParameterId });

        builder.Ignore("_evaluated");
        builder.Ignore("_evaluationResult");

        builder.OwnsOne(s => s.Expression, ece =>
        {
            ece.Property(p => p.Expression).HasColumnName("Expression");
        });

        builder.Property(p => p.ParameterValue).HasConversion(
            from => JsonSerializer.Serialize(from, null as JsonSerializerOptions),
            to => JsonSerializer.Deserialize<ParameterValue?>(to, null as JsonSerializerOptions) ?? new());

        builder.Property(p => p.DefaultValue).HasConversion(
            from => JsonSerializer.Serialize(from, null as JsonSerializerOptions),
            to => JsonSerializer.Deserialize<ParameterValue?>(to, null as JsonSerializerOptions) ?? new());

        builder.HasOne(x => x.Execution)
            .WithMany(x => x.ExecutionParameters)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
