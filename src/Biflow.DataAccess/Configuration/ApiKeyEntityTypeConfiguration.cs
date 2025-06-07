namespace Biflow.DataAccess.Configuration;

internal class ApiKeyEntityTypeConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("ApiKey")
            .HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(250);
        builder.Property(x => x.Value)
            .HasMaxLength(250);

        builder.HasIndex(p => p.Value)
            .IsUnique();
        
        builder.PrimitiveCollection(p => p.Scopes)
            .HasMaxLength(1000)
            .IsUnicode(false);
    }
}
