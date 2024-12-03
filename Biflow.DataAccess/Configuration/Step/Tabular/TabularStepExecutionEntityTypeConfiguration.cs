
namespace Biflow.DataAccess.Configuration;

internal class TabularStepExecutionEntityTypeConfiguration : IEntityTypeConfiguration<TabularStepExecution>
{
    public void Configure(EntityTypeBuilder<TabularStepExecution> builder)
    {
        builder.Property(x => x.TimeoutMinutes).HasColumnName("TimeoutMinutes");
        builder.Property(x => x.ConnectionId).HasColumnName("AnalysisServicesConnectionId");
    }
}
