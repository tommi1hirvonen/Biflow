using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class StepDataObjectEntityTypeConfiguration : IEntityTypeConfiguration<StepDataObject>
{
    public void Configure(EntityTypeBuilder<StepDataObject> builder)
    {
        builder.HasOne(x => x.Step)
            .WithMany(x => x.DataObjects)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.DataObject)
            .WithMany(x => x.Steps)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
