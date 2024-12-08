using Biflow.Core.Attributes;
using Biflow.Core.Attributes.Validation;
using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

[BlobStorageClient]
public class BlobStorageClient
{ 
    [JsonInclude]
    public Guid BlobStorageClientId { get; private set; }

    [Required]
    [MaxLength(250)]
    public string BlobStorageClientName { get; set; } = "";

    public BlobStorageConnectionMethod ConnectionMethod { get; private set; } =
        BlobStorageConnectionMethod.ConnectionString;

    [MaxLength(4000)]
    [JsonSensitive(WhenContains = "sig=")]
    [JsonInclude]
    public string? StorageAccountUrl { get; private set; }

    [MaxLength(4000)]
    [JsonSensitive]
    [JsonInclude]
    public string? ConnectionString { get; private set; }

    [JsonInclude]
    public Guid? AzureCredentialId { get; private set; }

    [JsonIgnore]
    public AzureCredential? AzureCredential { get; private set; }

    public const string ResourceUrl = "https://storage.azure.com//.default";

    public void UseCredential(AzureCredential azureCredential, string url)
    {
        ConnectionMethod = BlobStorageConnectionMethod.AzureCredential;
        StorageAccountUrl = url;
        ConnectionString = null;
        SetAzureCredential(azureCredential);
    }

    public void UseUrl(string url)
    {
        ConnectionMethod = BlobStorageConnectionMethod.Url;
        StorageAccountUrl = url;
        ConnectionString = null;
        SetAzureCredential(null);
    }

    public void UseConnectionString(string connectionString)
    {
        ConnectionMethod = BlobStorageConnectionMethod.ConnectionString;
        ConnectionString = connectionString;
        StorageAccountUrl = null;
        SetAzureCredential(null);
    }

    private void SetAzureCredential(AzureCredential? azureCredential)
    {
        AzureCredential = azureCredential;
        AzureCredentialId = azureCredential?.AzureCredentialId;
    }

    public BlobStorageExplorer CreateExplorer(ITokenService tokenService) => new(this, tokenService);
}
