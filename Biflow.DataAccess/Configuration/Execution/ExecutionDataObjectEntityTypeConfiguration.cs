namespace Biflow.DataAccess.Configuration;

internal class ExecutionDataObjectEntityTypeConfiguration : IEntityTypeConfiguration<ExecutionDataObject>
{
    public void Configure(EntityTypeBuilder<ExecutionDataObject> builder)
    {
        builder.ToTable("ExecutionDataObject");
        builder.HasKey(x => new { x.ExecutionId, x.ObjectId });
        builder.Property(x => x.ObjectUri)
            .HasMaxLength(500)
            .IsUnicode(false);
        builder.HasOne(x => x.Execution)
            .WithMany(x => x.DataObjects)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
