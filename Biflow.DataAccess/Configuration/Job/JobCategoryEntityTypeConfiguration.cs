namespace Biflow.DataAccess.Configuration;

internal class JobCategoryEntityTypeConfiguration : IEntityTypeConfiguration<JobCategory>
{
    public void Configure(EntityTypeBuilder<JobCategory> builder)
    {
        builder.ToTable("JobCategory")
            .HasKey(x => x.CategoryId);

        builder.Property(x => x.CategoryId).HasColumnName("JobCategoryId");
        builder.Property(x => x.CategoryName).HasColumnName("JobCategoryName");

        builder.HasIndex(p => p.CategoryName, "UQ_JobCategory")
            .IsUnique();
    }
}
