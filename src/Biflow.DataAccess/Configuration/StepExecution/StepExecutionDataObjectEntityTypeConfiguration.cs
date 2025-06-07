namespace Biflow.DataAccess.Configuration;

internal class StepExecutionDataObjectEntityTypeConfiguration : IEntityTypeConfiguration<StepExecutionDataObject>
{
    public void Configure(EntityTypeBuilder<StepExecutionDataObject> builder)
    {
        builder.ToTable("ExecutionStepDataObject")
            .HasKey(x => new { x.ExecutionId, x.StepId, x.ObjectId, x.ReferenceType });

        builder.HasOne(x => x.DataObject)
            .WithMany(x => x.StepExecutions)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.StepExecution)
            .WithMany(x => x.DataObjects)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
