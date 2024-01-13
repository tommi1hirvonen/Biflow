namespace Biflow.DataAccess.Configuration;

internal class ExecutionConcurrencyEntityTypeConfiguration : IEntityTypeConfiguration<ExecutionConcurrency>
{
    public void Configure(EntityTypeBuilder<ExecutionConcurrency> builder)
    {
        builder.ToTable("ExecutionConcurrency");
        builder.HasKey(c => new { c.ExecutionId, c.StepType });
        builder.HasOne(x => x.Execution)
            .WithMany(x => x.ExecutionConcurrencies)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
