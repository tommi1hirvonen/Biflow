namespace Biflow.DataAccess.Configuration;

public class DatasetStepExecutionEntityTypeConfiguration : IEntityTypeConfiguration<DatasetStepExecution>
{
    public void Configure(EntityTypeBuilder<DatasetStepExecution> builder)
    {
        builder.Property(p => p.AzureCredentialId).HasColumnName("AzureCredentialId");
        builder.Property(p => p.DatasetGroupId).IsUnicode(false);
        builder.Property(p => p.DatasetId).IsUnicode(false);
    }
}