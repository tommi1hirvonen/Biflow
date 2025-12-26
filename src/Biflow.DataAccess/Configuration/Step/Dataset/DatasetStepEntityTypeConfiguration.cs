namespace Biflow.DataAccess.Configuration;

public class DatasetStepEntityTypeConfiguration : IEntityTypeConfiguration<DatasetStep>
{
    public void Configure(EntityTypeBuilder<DatasetStep> builder)
    {
        builder.Property(p => p.FabricWorkspaceId).HasColumnName("FabricWorkspaceId");
        builder.Property(p => p.DatasetId).HasColumnName("DatasetId").IsUnicode(false);
        builder.Property(p => p.DatasetName).HasColumnName("DatasetName");
    }
}