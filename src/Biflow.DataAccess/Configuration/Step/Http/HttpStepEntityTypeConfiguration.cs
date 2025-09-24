using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Biflow.DataAccess.Configuration;

internal class HttpStepEntityTypeConfiguration : IEntityTypeConfiguration<HttpStep>
{
    public void Configure(EntityTypeBuilder<HttpStep> builder)
    {
        builder.Property(x => x.TimeoutMinutes).HasColumnName("TimeoutMinutes");
        builder.Property(x => x.Url).HasColumnName("HttpUrl").IsUnicode(false);
        builder.Property(x => x.Method).HasColumnName("HttpMethod");
        builder.Property(x => x.Body).HasColumnName("HttpBody");
        builder.Property(x => x.BodyFormat).HasColumnName("HttpBodyFormat");
        builder.Property(x => x.Headers).HasColumnName("HttpHeaders").IsUnicode(false);
        builder.Property(x => x.DisableAsyncPattern).HasColumnName("HttpDisableAsyncPattern");
        builder.Property(x => x.Headers).HasConversion(
            from => JsonSerializer.Serialize(from, null as JsonSerializerOptions),
            to => JsonSerializer.Deserialize<List<HttpHeader>>(to, null as JsonSerializerOptions) ?? new(),
            new ValueComparer<List<HttpHeader>>(
                (x, y) => x != null && y != null && x.SequenceEqual(y),
                x => x.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                x => x.ToList()));
    }
}
