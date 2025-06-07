namespace Biflow.DataAccess.Configuration;

internal class StepDataObjectEntityTypeConfiguration : IEntityTypeConfiguration<StepDataObject>
{
    public void Configure(EntityTypeBuilder<StepDataObject> builder)
    {
        builder.ToTable("StepDataObject")
            .HasKey(x => new { x.StepId, x.ObjectId, x.ReferenceType });

        builder.HasOne(x => x.Step)
            .WithMany(x => x.DataObjects)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.DataObject)
            .WithMany(x => x.Steps)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
