using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class JobConcurrencyEntityTypeConfiguration : IEntityTypeConfiguration<JobConcurrency>
{
    public void Configure(EntityTypeBuilder<JobConcurrency> builder)
    {
        builder.HasOne(x => x.Job)
            .WithMany(x => x.JobConcurrencies)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
