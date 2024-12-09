using System.Text.Json;

namespace Biflow.DataAccess.Configuration;

internal class StepParameterEntityTypeConfiguration : IEntityTypeConfiguration<StepParameterBase>
{
    public void Configure(EntityTypeBuilder<StepParameterBase> builder)
    {
        builder.ToTable("StepParameter")
            .HasKey(x => x.ParameterId);

        builder.Property(x => x.StepId)
            .HasColumnName("StepId");

        builder.Property(p => p.ParameterValue).HasConversion(
            from => JsonSerializer.Serialize(from, null as JsonSerializerOptions),
            to => JsonSerializer.Deserialize<ParameterValue?>(to, null as JsonSerializerOptions) ?? new());

        builder.Ignore(x => x.JobParameters);

        builder.HasOne(p => p.InheritFromJobParameter)
            .WithMany(p => p.InheritingStepParameters)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasDiscriminator<ParameterType>("ParameterType")
            .HasValue<SqlStepParameter>(ParameterType.Sql)
            .HasValue<PackageStepParameter>(ParameterType.Package)
            .HasValue<JobStepParameter>(ParameterType.Job)
            .HasValue<ExeStepParameter>(ParameterType.Exe)
            .HasValue<FunctionStepParameter>(ParameterType.Function)
            .HasValue<PipelineStepParameter>(ParameterType.Pipeline)
            .HasValue<EmailStepParameter>(ParameterType.Email)
            .HasValue<DatabricksStepParameter>(ParameterType.DatabricksNotebook)
            .HasValue<FabricStepParameter>(ParameterType.Fabric);

        builder.OwnsOne(s => s.Expression, ece =>
        {
            ece.Property(p => p.Expression).HasColumnName("Expression");
        });
    }
}
