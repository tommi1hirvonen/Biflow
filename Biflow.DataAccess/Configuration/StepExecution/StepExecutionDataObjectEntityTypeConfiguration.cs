using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class StepExecutionDataObjectEntityTypeConfiguration : IEntityTypeConfiguration<StepExecutionDataObject>
{
    public void Configure(EntityTypeBuilder<StepExecutionDataObject> builder)
    {
        builder.HasOne(x => x.DataObject)
            .WithMany(x => x.StepExecutions)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.StepExecution)
            .WithMany(x => x.DataObjects)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
