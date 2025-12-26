namespace Biflow.DataAccess.Configuration;

public class DataflowStepEntityTypeConfiguration : IEntityTypeConfiguration<DataflowStep>
{
    public void Configure(EntityTypeBuilder<DataflowStep> builder)
    {
        builder.Property(p => p.TimeoutMinutes).HasColumnName("TimeoutMinutes");
        builder.Property(p => p.FabricWorkspaceId).HasColumnName("FabricWorkspaceId");
        builder.Property(p => p.DataflowId).HasColumnName("DataflowId").IsUnicode(false);
        builder.Property(p => p.DataflowName).HasColumnName("DataflowName");
    }
}