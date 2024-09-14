namespace Biflow.DataAccess.Configuration;

internal class AnalysisServicesConnectionEntityTypeConfiguration : IEntityTypeConfiguration<AnalysisServicesConnection>
{
    public void Configure(EntityTypeBuilder<AnalysisServicesConnection> builder)
    {
        builder.Property(x => x.CredentialId).HasColumnName("CredentialId");
    }
}
