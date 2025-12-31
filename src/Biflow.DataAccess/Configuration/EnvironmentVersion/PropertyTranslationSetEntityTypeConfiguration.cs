namespace Biflow.DataAccess.Configuration;

public class PropertyTranslationSetEntityTypeConfiguration : IEntityTypeConfiguration<PropertyTranslationSet>
{
    public void Configure(EntityTypeBuilder<PropertyTranslationSet> builder)
    {
        builder.ToTable("PropertyTranslationSet").HasKey(x => x.PropertyTranslationSetId);
    }
}