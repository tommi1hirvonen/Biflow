namespace Biflow.DataAccess.Configuration;

public class DatasetStepEntityTypeConfiguration : IEntityTypeConfiguration<DatasetStep>
{
    public void Configure(EntityTypeBuilder<DatasetStep> builder)
    {
        builder.Property(p => p.AzureCredentialId).HasColumnName("AzureCredentialId");
        builder.Property(p => p.DatasetGroupId).IsUnicode(false);
        builder.Property(p => p.DatasetId).IsUnicode(false);
    }
}