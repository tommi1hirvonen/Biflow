namespace Biflow.DataAccess.Configuration;

internal class BlobStorageClientEntityTypeConfiguration : IEntityTypeConfiguration<BlobStorageClient>
{
    public void Configure(EntityTypeBuilder<BlobStorageClient> builder)
    {
        builder.HasOne(x => x.AppRegistration)
            .WithMany(x => x.BlobStorageClients)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
