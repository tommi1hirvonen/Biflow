using Biflow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class StepParameterEntityTypeConfiguration : IEntityTypeConfiguration<StepParameterBase>
{
    public void Configure(EntityTypeBuilder<StepParameterBase> builder)
    {
        builder.ToTable("StepParameter")
            .HasKey(x => x.ParameterId);

        builder.Property(x => x.StepId)
            .HasColumnName("StepId");

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
            .HasValue<EmailStepParameter>(ParameterType.Email);

        builder.OwnsOne(s => s.Expression, ece =>
        {
            ece.Property(p => p.Expression).HasColumnName("Expression");
        });
    }
}
