namespace Biflow.DataAccess.Configuration;

public class ProxyEntityTypeConfiguration : IEntityTypeConfiguration<Proxy>
{
    public void Configure(EntityTypeBuilder<Proxy> builder)
    {
        builder.ToTable("Proxy")
            .HasKey(x => x.ProxyId);
        
        builder.Property(x => x.ProxyUrl).IsUnicode(false);
    }
}