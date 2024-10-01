using System.Text.Json;

namespace Biflow.DataAccess.Configuration;

internal class DbNotebookStepEntityTypeConfiguration : IEntityTypeConfiguration<DbNotebookStep>
{
    public void Configure(EntityTypeBuilder<DbNotebookStep> builder)
    {
        builder.Property(x => x.TimeoutMinutes).HasColumnName("TimeoutMinutes");
        builder.Property(p => p.ClusterConfiguration).HasConversion(
            from => JsonSerializer.Serialize(from, null as JsonSerializerOptions),
            to => JsonSerializer.Deserialize<ClusterConfiguration?>(to, null as JsonSerializerOptions)
            ?? new NewClusterConfiguration());
        builder.Property(p => p.ClusterConfiguration)
            .HasMaxLength(-1)
            .IsUnicode();
    }
}
