using System.Text.Json;

namespace Biflow.DataAccess.Configuration;

internal class HttpStepExecutionEntityTypeConfiguration : IEntityTypeConfiguration<HttpStepExecution>
{
    public void Configure(EntityTypeBuilder<HttpStepExecution> builder)
    {
        builder.Property(x => x.TimeoutMinutes).HasColumnName("TimeoutMinutes");
        builder.Property(x => x.Url).HasColumnName("HttpUrl").IsUnicode(false);
        builder.Property(x => x.Method).HasColumnName("HttpMethod");
        builder.Property(x => x.Body).HasColumnName("HttpBody");
        builder.Property(x => x.Headers).HasColumnName("HttpHeaders").IsUnicode(false);
        builder.Property(x => x.DisableAsyncPattern).HasColumnName("HttpDisableAsyncPattern");
        builder.Property(x => x.Headers).HasConversion(
            from => JsonSerializer.Serialize(from, null as JsonSerializerOptions),
            to => JsonSerializer.Deserialize<HttpHeader[]>(to, null as JsonSerializerOptions)
                  ?? Array.Empty<HttpHeader>());
    }
}
