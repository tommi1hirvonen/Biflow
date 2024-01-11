using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class ExecutionDataObjectEntityTypeConfiguration : IEntityTypeConfiguration<ExecutionDataObject>
{
    public void Configure(EntityTypeBuilder<ExecutionDataObject> builder)
    {
        builder.HasOne(x => x.Execution)
            .WithMany(x => x.DataObjects)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
