using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class ExecutionParameterEntityTypeConfiguration : IEntityTypeConfiguration<ExecutionParameter>
{
    public void Configure(EntityTypeBuilder<ExecutionParameter> builder)
    {
        builder.OwnsOne(s => s.Expression, ece =>
        {
            ece.Property(p => p.Expression).HasColumnName("Expression");
        });
        builder.HasOne(x => x.Execution)
            .WithMany(x => x.ExecutionParameters)
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}
