namespace Biflow.DataAccess.Configuration;

internal class TagEntityTypeConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tag")
            .HasKey(x => x.TagId);

        builder.HasDiscriminator<TagType>("TagType")
            .HasValue<StepTag>(TagType.Step)
            .HasValue<JobTag>(TagType.Job);

        builder.HasIndex(p => new { p.TagName, p.TagType }, "UQ_TagName").IsUnique();
    }
}
