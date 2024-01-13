using Biflow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class JobStepEntityTypeConfiguration : IEntityTypeConfiguration<JobStep>
{
    public void Configure(EntityTypeBuilder<JobStep> builder)
    {
        builder.HasOne(step => step.JobToExecute)
            .WithMany(job => job.JobSteps)
            .HasForeignKey(step => step.JobToExecuteId)
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}
