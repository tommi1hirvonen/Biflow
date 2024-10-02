using System.Text.Json;

namespace Biflow.DataAccess.Configuration;

internal class DatabricksStepExecutionEntityTypeConfiguration : IEntityTypeConfiguration<DatabricksStepExecution>
{
    public void Configure(EntityTypeBuilder<DatabricksStepExecution> builder)
    {
        builder.Property(x => x.TimeoutMinutes).HasColumnName("TimeoutMinutes");
        builder.Property(p => p.ClusterConfiguration).HasConversion(
            from => JsonSerializer.Serialize(from, null as JsonSerializerOptions),
            to => JsonSerializer.Deserialize<ClusterConfiguration?>(to, null as JsonSerializerOptions)
            ?? new NewClusterConfiguration());
    }
}
