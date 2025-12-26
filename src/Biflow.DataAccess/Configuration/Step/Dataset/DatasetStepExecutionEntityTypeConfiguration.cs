namespace Biflow.DataAccess.Configuration;

public class DatasetStepExecutionEntityTypeConfiguration : IEntityTypeConfiguration<DatasetStepExecution>
{
    public void Configure(EntityTypeBuilder<DatasetStepExecution> builder)
    {
        builder.Property(p => p.FabricWorkspaceId).HasColumnName("FabricWorkspaceId");
        builder.Property(p => p.DatasetId).HasColumnName("DatasetId").IsUnicode(false);
        builder.Property(p => p.DatasetName).HasColumnName("DatasetName");
    }
}