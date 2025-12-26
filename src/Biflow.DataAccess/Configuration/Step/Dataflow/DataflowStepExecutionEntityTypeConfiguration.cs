namespace Biflow.DataAccess.Configuration;

public class DataflowStepExecutionEntityTypeConfiguration : IEntityTypeConfiguration<DataflowStepExecution>
{
    public void Configure(EntityTypeBuilder<DataflowStepExecution> builder)
    {
        builder.Property(p => p.TimeoutMinutes).HasColumnName("TimeoutMinutes");
        builder.Property(p => p.FabricWorkspaceId).HasColumnName("FabricWorkspaceId");
        builder.Property(p => p.DataflowId).HasColumnName("DataflowId").IsUnicode(false);
        builder.Property(p => p.DataflowName).HasColumnName("DataflowName");
    }
}