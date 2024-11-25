using System.Text.Json;

namespace Biflow.DataAccess.Configuration;

public class ScdTableEntityTypeConfiguration : IEntityTypeConfiguration<ScdTable>
{
    public void Configure(EntityTypeBuilder<ScdTable> builder)
    {
        builder.ToTable("ScdTable");
        builder.HasKey(p => p.ScdTableId);
        builder.PrimitiveCollection(p => p.NaturalKeyColumns)
            .HasMaxLength(-1)
            .IsUnicode();
        builder.Property(p => p.SchemaDriftConfiguration).HasConversion(
            from => JsonSerializer.Serialize(from, null as JsonSerializerOptions),
            to => JsonSerializer.Deserialize<SchemaDriftConfiguration?>(to, null as JsonSerializerOptions)
                  ?? new SchemaDriftDisabledConfiguration());
        builder.Property(p => p.SchemaDriftConfiguration)
            .HasMaxLength(-1)
            .IsUnicode();
        builder.HasOne(p => p.Connection)
            .WithMany(c => c.ScdTables)
            .OnDelete(DeleteBehavior.Cascade);
    }
}