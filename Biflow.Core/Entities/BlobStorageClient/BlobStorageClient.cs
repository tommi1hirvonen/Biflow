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

    public BlobStorageConnectionMethod ConnectionMethod { get; private set; } = BlobStorageConnectionMethod.ConnectionString;

    [MaxLength(4000)]
    [JsonSensitive(WhenContains = "sig=")]
    public string? StorageAccountUrl { get; private set; }

    [MaxLength(4000)]
    [JsonSensitive]
    public string? ConnectionString { get; private set; }

    public Guid? AppRegistrationId { get; private set; }

    [JsonIgnore]
    public AppRegistration? AppRegistration { get; private set; }

    public const string ResourceUrl = "https://storage.azure.com//.default";

    public void UseAppRegistration(AppRegistration appRegistration, string url)
    {
        ConnectionMethod = BlobStorageConnectionMethod.AppRegistration;
        StorageAccountUrl = url;
        ConnectionString = null;
        SetAppRegistration(appRegistration);
    }

    public void UseUrl(string url)
    {
        ConnectionMethod = BlobStorageConnectionMethod.Url;
        StorageAccountUrl = url;
        ConnectionString = null;
        SetAppRegistration(null);
    }

    public void UseConnectionString(string connectionString)
    {
        ConnectionMethod = BlobStorageConnectionMethod.ConnectionString;
        ConnectionString = connectionString;
        StorageAccountUrl = null;
        SetAppRegistration(null);
    }

    private void SetAppRegistration(AppRegistration? appRegistration)
    {
        AppRegistration = appRegistration;
        AppRegistrationId = appRegistration?.AppRegistrationId;
    }

    public BlobStorageExplorer CreateExplorer(ITokenService tokenService) => new(this, tokenService);
}
