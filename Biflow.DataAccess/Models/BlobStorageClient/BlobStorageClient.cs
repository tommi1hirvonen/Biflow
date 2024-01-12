using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Biflow.DataAccess.Models.Validation;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Biflow.DataAccess.Models;

[Table("BlobStorageClient")]
[BlobStorageClient]
public class BlobStorageClient(ITokenService tokenService)
{
    private readonly ITokenService _tokenService = tokenService;

    public BlobStorageClient(AppDbContext dbContext) : this(dbContext.TokenService) { }

    [JsonConstructor]
    private BlobStorageClient() : this((ITokenService)null!) { }

    [Key]
    [JsonInclude]
    public Guid BlobStorageClientId { get; private set; }

    [Required]
    [MaxLength(250)]
    public string BlobStorageClientName { get; set; } = "";

    public BlobStorageConnectionMethod ConnectionMethod { get; set; }

    [MaxLength(4000)]
    [JsonSensitive(WhenContains = "sig=")]
    public string? StorageAccountUrl { get; set; }

    [MaxLength(4000)]
    [JsonSensitive]
    public string? ConnectionString { get; set; }

    public Guid? AppRegistrationId { get; set; }

    [JsonIgnore]
    public AppRegistration? AppRegistration { get; set; }

    public const string ResourceUrl = "https://storage.azure.com//.default";

    public string GetStorageAccountName() => GetBlobServiceClient().AccountName;

    public async Task<IEnumerable<BlobContainerItem>> GetContainersAsync(CancellationToken cancellationToken = default)
    {
        var client = GetBlobServiceClient();
        var containers = await client
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

    private BlobServiceClient GetBlobServiceClient() => ConnectionMethod switch
    {
        BlobStorageConnectionMethod.Url => GetUrlBlobServiceClient(),
        BlobStorageConnectionMethod.ConnectionString => GetConnectionStringBlobServiceClient(),
        BlobStorageConnectionMethod.AppRegistration => GetAppRegistrationBlobServiceClient(),
        _ => throw new ArgumentException($"Unrecognized {nameof(ConnectionMethod)} value {ConnectionMethod}")
    };

    private BlobServiceClient GetUrlBlobServiceClient()
    {
        ArgumentNullException.ThrowIfNull(StorageAccountUrl);
        var uri = new Uri(StorageAccountUrl);
        return new BlobServiceClient(uri);
    }

    private BlobServiceClient GetConnectionStringBlobServiceClient()
    {
        ArgumentNullException.ThrowIfNull(ConnectionString);
        return new BlobServiceClient(ConnectionString);
    }

    private BlobServiceClient GetAppRegistrationBlobServiceClient()
    {
        ArgumentNullException.ThrowIfNull(AppRegistration);
        ArgumentNullException.ThrowIfNull(StorageAccountUrl);
        var token = new AzureTokenCredential(_tokenService, AppRegistration, ResourceUrl);
        var uri = new Uri(StorageAccountUrl);
        return new BlobServiceClient(uri, token);
    }
}
