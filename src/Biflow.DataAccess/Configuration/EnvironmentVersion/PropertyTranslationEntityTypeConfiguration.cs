using System.Text.Json;

namespace Biflow.DataAccess.Configuration;

public class PropertyTranslationEntityTypeConfiguration : IEntityTypeConfiguration<PropertyTranslation>
{
    public void Configure(EntityTypeBuilder<PropertyTranslation> builder)
    {
        builder.ToTable("PropertyTranslation").HasKey(x => x.PropertyTranslationId);
        builder.PrimitiveCollection(p => p.PropertyPaths)
            .HasMaxLength(-1)
            .IsUnicode();;
        builder.Property(p => p.NewValue).HasConversion(
            from => JsonSerializer.Serialize(from, null as JsonSerializerOptions),
            to => JsonSerializer.Deserialize<ParameterValue?>(to, null as JsonSerializerOptions) ?? new ParameterValue());
        builder.HasOne(x => x.PropertyTranslationSet)
            .WithMany(x => x.PropertyTranslations)
            .HasForeignKey(x => x.PropertyTranslationSetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}