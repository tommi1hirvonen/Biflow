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

    public BlobStorageExplorer CreateExplorer(ITokenService tokenService) => new(this, tokenService);
}
