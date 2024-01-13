using Biflow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class ExecutionParameterEntityTypeConfiguration : IEntityTypeConfiguration<ExecutionParameter>
{
    public void Configure(EntityTypeBuilder<ExecutionParameter> builder)
    {
        builder.ToTable("ExecutionParameter")
            .HasKey(x => new { x.ExecutionId, x.ParameterId });

        builder.OwnsOne(s => s.Expression, ece =>
        {
            ece.Property(p => p.Expression).HasColumnName("Expression");
        });

        builder.Property(x => x.DefaultValue)
            .HasColumnType("sql_variant");

        builder.HasOne(x => x.Execution)
            .WithMany(x => x.ExecutionParameters)
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}
