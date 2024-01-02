using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class SqlStepEntityTypeConfiguration : IEntityTypeConfiguration<SqlStep>
{
    public void Configure(EntityTypeBuilder<SqlStep> builder)
    {
        builder.HasOne(x => x.ResultCaptureJobParameter)
            .WithMany(x => x.CapturingSteps)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
