using Biflow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class JobConcurrencyEntityTypeConfiguration : IEntityTypeConfiguration<JobConcurrency>
{
    public void Configure(EntityTypeBuilder<JobConcurrency> builder)
    {
        builder.ToTable("JobConcurrency")
            .HasKey(x => new { x.JobId, x.StepType });

        builder.HasOne(x => x.Job)
            .WithMany(x => x.JobConcurrencies)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
