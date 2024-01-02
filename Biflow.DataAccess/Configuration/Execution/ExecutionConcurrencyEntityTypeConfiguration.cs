using Biflow.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Biflow.DataAccess.Configuration;

internal class ExecutionConcurrencyEntityTypeConfiguration : IEntityTypeConfiguration<ExecutionConcurrency>
{
    public void Configure(EntityTypeBuilder<ExecutionConcurrency> builder)
    {
        builder.HasOne(x => x.Execution)
            .WithMany(x => x.ExecutionConcurrencies)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
