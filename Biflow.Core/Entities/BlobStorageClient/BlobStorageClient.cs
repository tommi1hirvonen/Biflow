using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Biflow.Core.Attributes;
using Biflow.Core.Attributes.Validation;
using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Biflow.Core.Entities;

[Table("BlobStorageClient")]
[BlobStorageClient]
public class BlobStorageClient
{ 
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

    public BlobStorageExplorer CreateExplorer(ITokenService tokenService) => new(this, tokenService);
}
