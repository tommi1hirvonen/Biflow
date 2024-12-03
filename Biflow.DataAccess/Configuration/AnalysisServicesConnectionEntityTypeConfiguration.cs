namespace Biflow.DataAccess.Configuration;

internal class AnalysisServicesConnectionEntityTypeConfiguration : IEntityTypeConfiguration<AnalysisServicesConnection>
{
    public void Configure(EntityTypeBuilder<AnalysisServicesConnection> builder)
    {
        builder.ToTable("AnalysisServicesConnection");
        builder.HasKey(p => p.ConnectionId);
        builder.Property(x => x.ConnectionString)
            .HasMaxLength(-1)
            .IsUnicode();
        builder.Property(x => x.CredentialId).HasColumnName("CredentialId");
    }
}
