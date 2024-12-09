namespace Biflow.DataAccess.Configuration;

public class FabricStepEntityTypeConfiguration : IEntityTypeConfiguration<FabricStep>
{
    public void Configure(EntityTypeBuilder<FabricStep> builder)
    {
        builder.Property(p => p.TimeoutMinutes).HasColumnName("TimeoutMinutes");
        builder.Property(p => p.WorkspaceId).HasColumnName("FabricWorkspaceId");
        builder.Property(p => p.WorkspaceName).HasColumnName("FabricWorkspaceName");
        builder.Property(p => p.ItemId).HasColumnName("FabricItemId");
        builder.Property(p => p.ItemName).HasColumnName("FabricItemName");
        builder.Property(p => p.ItemType).HasColumnName("FabricItemType");
        builder.Property(p => p.AzureCredentialId).HasColumnName("AzureCredentialId");
    }
}