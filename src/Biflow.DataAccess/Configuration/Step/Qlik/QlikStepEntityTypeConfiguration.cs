using System.Text.Json;

namespace Biflow.DataAccess.Configuration;

internal class QlikStepEntityTypeConfiguration : IEntityTypeConfiguration<QlikStep>
{
    public void Configure(EntityTypeBuilder<QlikStep> builder)
    {
        builder.Property(x => x.TimeoutMinutes).HasColumnName("TimeoutMinutes");
        builder.Property(p => p.QlikStepSettings).HasConversion(
            from => JsonSerializer.Serialize(from, null as JsonSerializerOptions),
            to => JsonSerializer.Deserialize<QlikStepSettings?>(to, null as JsonSerializerOptions)
            ?? new QlikAppReloadSettings());
        builder.Property(p => p.QlikStepSettings)
            .HasMaxLength(-1)
            .IsUnicode();
    }
}
