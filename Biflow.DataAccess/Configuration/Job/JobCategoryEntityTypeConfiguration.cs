namespace Biflow.DataAccess.Configuration;

internal class JobCategoryEntityTypeConfiguration : IEntityTypeConfiguration<JobCategory>
{
    public void Configure(EntityTypeBuilder<JobCategory> builder)
    {
        builder.HasIndex(p => p.CategoryName, "UQ_JobCategory")
            .IsUnique();
    }
}
