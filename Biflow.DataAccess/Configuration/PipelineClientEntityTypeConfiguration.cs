namespace Biflow.DataAccess.Configuration;

internal class PipelineClientEntityTypeConfiguration : IEntityTypeConfiguration<PipelineClient>
{
    public void Configure(EntityTypeBuilder<PipelineClient> builder)
    {
        builder.HasDiscriminator<PipelineClientType>("PipelineClientType")
            .HasValue<DataFactory>(PipelineClientType.DataFactory)
            .HasValue<SynapseWorkspace>(PipelineClientType.Synapse);
    }
}
