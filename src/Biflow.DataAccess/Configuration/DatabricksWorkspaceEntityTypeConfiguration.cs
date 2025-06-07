namespace Biflow.DataAccess.Configuration;

internal class DatabricksWorkspaceEntityTypeConfiguration : IEntityTypeConfiguration<DatabricksWorkspace>
{
    public void Configure(EntityTypeBuilder<DatabricksWorkspace> builder)
    {
        builder.ToTable("DatabricksWorkspace")
            .HasKey(x => x.WorkspaceId);

        builder.HasMany(c => c.Steps)
            .WithOne(s => s.DatabricksWorkspace);
    }
}
