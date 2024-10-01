using System.Text.Json;

namespace Biflow.DataAccess.Configuration;

internal class DbNotebookStepExecutionEntityTypeConfiguration : IEntityTypeConfiguration<DbNotebookStepExecution>
{
    public void Configure(EntityTypeBuilder<DbNotebookStepExecution> builder)
    {
        builder.Property(x => x.TimeoutMinutes).HasColumnName("TimeoutMinutes");
        builder.Property(p => p.ClusterConfiguration).HasConversion(
            from => JsonSerializer.Serialize(from, null as JsonSerializerOptions),
            to => JsonSerializer.Deserialize<ClusterConfiguration?>(to, null as JsonSerializerOptions)
            ?? new NewClusterConfiguration());
    }
}
