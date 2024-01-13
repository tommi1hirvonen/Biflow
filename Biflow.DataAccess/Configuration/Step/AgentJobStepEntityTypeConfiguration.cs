
namespace Biflow.DataAccess.Configuration;

internal class AgentJobStepEntityTypeConfiguration : IEntityTypeConfiguration<AgentJobStep>
{
    public void Configure(EntityTypeBuilder<AgentJobStep> builder)
    {
        builder.Property(x => x.TimeoutMinutes).HasColumnName("TimeoutMinutes");
        builder.Property(x => x.ConnectionId).HasColumnName("ConnectionId");
    }
}
