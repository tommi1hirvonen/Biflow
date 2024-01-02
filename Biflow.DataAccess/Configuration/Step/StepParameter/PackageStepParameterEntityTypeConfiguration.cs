using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class PackageStepParameterEntityTypeConfiguration : IEntityTypeConfiguration<PackageStepParameter>
{
    public void Configure(EntityTypeBuilder<PackageStepParameter> builder)
    {
        builder.HasOne(p => p.Step)
            .WithMany(p => p.StepParameters)
            .OnDelete(DeleteBehavior.ClientCascade);

        builder.HasIndex(x => new { x.StepId, x.ParameterLevel, x.ParameterName }, "UQ_StepParameter")
            .HasFilter(null)
            .IsUnique();
    }
}
