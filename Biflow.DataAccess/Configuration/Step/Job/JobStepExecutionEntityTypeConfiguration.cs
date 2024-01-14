using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using static Biflow.Core.Entities.JobStepExecution;

namespace Biflow.DataAccess.Configuration;

internal class JobStepExecutionEntityTypeConfiguration : IEntityTypeConfiguration<JobStepExecution>
{
    public void Configure(EntityTypeBuilder<JobStepExecution> builder)
    {
        builder.Ignore(x => x.TagFilters);

        builder.Property<List<TagFilter>>("_tagFilters")
            .HasColumnName("TagFilters")
            .HasConversion(
                from => JsonSerializer.Serialize(from, null as JsonSerializerOptions),
                to => JsonSerializer.Deserialize<List<TagFilter>>(string.IsNullOrEmpty(to) ? "[]" : to, null as JsonSerializerOptions) ?? new(),
                new ValueComparer<List<TagFilter>>(
                    (x, y) => x != null && y != null && x.SequenceEqual(y),
                    x => x.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    x => x.ToList()));
    }
}
