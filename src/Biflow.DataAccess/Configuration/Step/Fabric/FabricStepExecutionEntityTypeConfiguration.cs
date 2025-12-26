namespace Biflow.DataAccess.Configuration;

public class FabricStepExecutionEntityTypeConfiguration : IEntityTypeConfiguration<FabricStepExecution>
{
    public void Configure(EntityTypeBuilder<FabricStepExecution> builder)
    {
        builder.Property(p => p.TimeoutMinutes).HasColumnName("TimeoutMinutes");
        builder.Property(p => p.FabricWorkspaceId).HasColumnName("FabricWorkspaceId");
        builder.Property(p => p.ItemId).HasColumnName("FabricItemId");
        builder.Property(p => p.ItemName).HasColumnName("FabricItemName");
        builder.Property(p => p.ItemType).HasColumnName("FabricItemType");
    }
}