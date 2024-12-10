namespace Biflow.DataAccess.Configuration;

public class DataflowStepEntityTypeConfiguration : IEntityTypeConfiguration<DataflowStep>
{
    public void Configure(EntityTypeBuilder<DataflowStep> builder)
    {
        builder.Property(p => p.TimeoutMinutes).HasColumnName("TimeoutMinutes");
        builder.Property(p => p.AzureCredentialId).HasColumnName("AzureCredentialId");
        builder.Property(p => p.WorkspaceId).HasColumnName("DataflowGroupId").IsUnicode(false);
        builder.Property(p => p.DataflowId).HasColumnName("DataflowId").IsUnicode(false);
    }
}