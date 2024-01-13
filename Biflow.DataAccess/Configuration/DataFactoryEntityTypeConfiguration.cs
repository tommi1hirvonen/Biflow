namespace Biflow.DataAccess.Configuration;

internal class DataFactoryEntityTypeConfiguration : IEntityTypeConfiguration<DataFactory>
{
    public void Configure(EntityTypeBuilder<DataFactory> builder)
    {
        builder.Property(x => x.SubscriptionId).HasColumnName("SubscriptionId");
        builder.Property(x => x.ResourceGroupName).HasColumnName("ResourceGroupName");
        builder.Property(x => x.ResourceName).HasColumnName("ResourceName");
    }
}
