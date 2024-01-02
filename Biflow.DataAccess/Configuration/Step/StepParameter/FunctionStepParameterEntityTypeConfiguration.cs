using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class FunctionStepParameterEntityTypeConfiguration : IEntityTypeConfiguration<FunctionStepParameter>
{
    public void Configure(EntityTypeBuilder<FunctionStepParameter> builder)
    {
        builder.HasOne(p => p.Step)
            .WithMany(p => p.StepParameters)
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}
