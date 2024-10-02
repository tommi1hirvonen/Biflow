using System.Text.Json;

namespace Biflow.DataAccess.Configuration;

internal class DatabricksStepExecutionEntityTypeConfiguration : IEntityTypeConfiguration<DatabricksStepExecution>
{
    public void Configure(EntityTypeBuilder<DatabricksStepExecution> builder)
    {
        builder.Property(x => x.TimeoutMinutes).HasColumnName("TimeoutMinutes");
        builder.Property(p => p.DatabricksStepSettings).HasConversion(
            from => JsonSerializer.Serialize(from, null as JsonSerializerOptions),
            to => JsonSerializer.Deserialize<DatabricksStepSettings?>(to, null as JsonSerializerOptions)
            ?? new DbNotebookStepSettings());
        builder.Property(p => p.DatabricksStepSettings)
            .HasMaxLength(-1)
            .IsUnicode();
    }
}
