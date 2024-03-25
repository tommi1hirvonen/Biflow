
namespace Biflow.DataAccess.Configuration;

internal class JobTagEntityTypeConfiguration : IEntityTypeConfiguration<JobTag>
{
    public void Configure(EntityTypeBuilder<JobTag> builder)
    {
        builder.HasMany(t => t.Jobs)
            .WithMany(s => s.Tags)
            .UsingEntity<Dictionary<string, object>>("JobTag",
            x => x.HasOne<Job>().WithMany().HasForeignKey("JobId").HasPrincipalKey(y => y.JobId).OnDelete(DeleteBehavior.Cascade),
            x => x.HasOne<JobTag>().WithMany().HasForeignKey("TagId").HasPrincipalKey(y => y.TagId).OnDelete(DeleteBehavior.Cascade));
    }
}
