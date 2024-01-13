using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace Biflow.DataAccess.Configuration;

internal class JobStepExecutionEntityTypeConfiguration : IEntityTypeConfiguration<JobStepExecution>
{
    public void Configure(EntityTypeBuilder<JobStepExecution> builder)
    {
        builder.Property(p => p.TagFilters)
            .HasConversion(
                from => JsonSerializer.Serialize(from, null as JsonSerializerOptions),
                to => JsonSerializer.Deserialize<List<JobStepExecution.TagFilter>>(string.IsNullOrEmpty(to) ? "[]" : to, null as JsonSerializerOptions) ?? new(),
                new ValueComparer<List<JobStepExecution.TagFilter>>(
                    (x, y) => x != null && y != null && x.SequenceEqual(y),
                    x => x.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    x => x.ToList()));
    }
}
