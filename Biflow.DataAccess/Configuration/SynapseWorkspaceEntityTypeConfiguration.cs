namespace Biflow.DataAccess.Configuration;

internal class SynapseWorkspaceEntityTypeConfiguration : IEntityTypeConfiguration<SynapseWorkspace>
{
    public void Configure(EntityTypeBuilder<SynapseWorkspace> builder)
    {
        builder.Property(x => x.SynapseWorkspaceUrl).IsUnicode(false);
        builder.Ignore("SynapseEndpoint");
    }
}
