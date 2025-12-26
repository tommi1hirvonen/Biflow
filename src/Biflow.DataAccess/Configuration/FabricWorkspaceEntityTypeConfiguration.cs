namespace Biflow.DataAccess.Configuration;

public class FabricWorkspaceEntityTypeConfiguration : IEntityTypeConfiguration<FabricWorkspace>
{
    public void Configure(EntityTypeBuilder<FabricWorkspace> builder)
    {
        builder.ToTable("FabricWorkspace").HasKey(x => x.FabricWorkspaceId);
    }
}