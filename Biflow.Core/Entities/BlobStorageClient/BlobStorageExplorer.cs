using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Biflow.Core.Interfaces;

namespace Biflow.Core.Entities;

public class BlobStorageExplorer(BlobStorageClient client, ITokenService tokenService)
{
    public string StorageAccountName => GetBlobServiceClient().AccountName;

    public async Task<IEnumerable<BlobContainerItem>> GetContainersAsync(CancellationToken cancellationToken = default)
    {
        var blobServiceClient = GetBlobServiceClient();
        var containers = await blobServiceClient
            .GetBlobContainersAsync(cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken);
        return containers;
    }

    public async Task<IEnumerable<BlobHierarchyItem>> GetItemsAsync(
        BlobContainerItem container,
        string? prefix = null,
        CancellationToken cancellationToken = default)
    {
        var blobServiceClient = GetBlobServiceClient();
        var containerClient = blobServiceClient.GetBlobContainerClient(container.Name);
        var items = await containerClient
            .GetBlobsByHierarchyAsync(delimiter: "/", prefix: prefix, cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken);
        return items;
    }

    private BlobServiceClient GetBlobServiceClient() => client.ConnectionMethod switch
    {
        BlobStorageConnectionMethod.Url => GetUrlBlobServiceClient(),
        BlobStorageConnectionMethod.ConnectionString => GetConnectionStringBlobServiceClient(),
        BlobStorageConnectionMethod.AzureCredential => GetAzureCredentialBlobServiceClient(),
        _ => throw new ArgumentException($"Unrecognized {nameof(client.ConnectionMethod)} value {client.ConnectionMethod}")
    };

    private BlobServiceClient GetUrlBlobServiceClient()
    {
        ArgumentNullException.ThrowIfNull(client.StorageAccountUrl);
        var uri = new Uri(client.StorageAccountUrl);
        return new BlobServiceClient(uri);
    }

    private BlobServiceClient GetConnectionStringBlobServiceClient()
    {
        ArgumentNullException.ThrowIfNull(client.ConnectionString);
        return new BlobServiceClient(client.ConnectionString);
    }

    private BlobServiceClient GetAzureCredentialBlobServiceClient()
    {
        ArgumentNullException.ThrowIfNull(client.AzureCredential);
        ArgumentNullException.ThrowIfNull(client.StorageAccountUrl);
        var token = client.AzureCredential.GetTokenServiceCredential(tokenService);
        var uri = new Uri(client.StorageAccountUrl);
        return new BlobServiceClient(uri, token);
    }
}
