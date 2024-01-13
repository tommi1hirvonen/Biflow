namespace Biflow.DataAccess.Configuration;

internal class BlobStorageClientEntityTypeConfiguration : IEntityTypeConfiguration<BlobStorageClient>
{
    public void Configure(EntityTypeBuilder<BlobStorageClient> builder)
    {
        builder.ToTable("BlobStorageClient")
            .HasKey(x => x.BlobStorageClientId);

        builder.HasOne(x => x.AppRegistration)
            .WithMany(x => x.BlobStorageClients)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
