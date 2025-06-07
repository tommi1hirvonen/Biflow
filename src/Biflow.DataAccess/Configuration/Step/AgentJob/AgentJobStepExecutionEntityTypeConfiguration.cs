
namespace Biflow.DataAccess.Configuration;

internal class AgentJobStepExecutionEntityTypeConfiguration : IEntityTypeConfiguration<AgentJobStepExecution>
{
    public void Configure(EntityTypeBuilder<AgentJobStepExecution> builder)
    {
        builder.Property(x => x.TimeoutMinutes).HasColumnName("TimeoutMinutes");
        builder.Property(x => x.ConnectionId).HasColumnName("ConnectionId");
    }
}
