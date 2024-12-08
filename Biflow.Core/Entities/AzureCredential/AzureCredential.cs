using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Azure.Core;

namespace Biflow.Core.Entities;

[JsonDerivedType(typeof(ServicePrincipalCredential), nameof(AzureCredentialType.ServicePrincipal))]
[JsonDerivedType(typeof(OrganizationalAccountCredential), nameof(AzureCredentialType.OrganizationalAccount))]
public abstract class AzureCredential(AzureCredentialType azureCredentialType)
{
    [Required]
    [JsonInclude]
    public Guid AzureCredentialId { get; private set; }

    [Required]
    [MaxLength(250)]
    public string? AzureCredentialName { get; set; }
    
    public AzureCredentialType AzureCredentialType { get; set; } = azureCredentialType;

    [Required]
    [MaxLength(36)]
    [MinLength(36)]
    public string TenantId { get; set; } = "";

    [Required]
    [MaxLength(36)]
    [MinLength(36)]
    public string ClientId { get; set; } = "";

    [JsonIgnore]
    public IEnumerable<DatasetStep> Steps { get; } = new List<DatasetStep>();

    [JsonIgnore]
    public IEnumerable<PipelineClient> PipelineClients { get; } = new List<PipelineClient>();

    [JsonIgnore]
    public IEnumerable<FunctionApp> FunctionApps { get; } = new List<FunctionApp>();

    [JsonIgnore]
    public IEnumerable<BlobStorageClient> BlobStorageClients { get; } = new List<BlobStorageClient>();

    [JsonIgnore]
    public IEnumerable<AccessToken> AccessTokens { get; } = new List<AccessToken>();

    internal const string PowerBiResourceUrl = "https://analysis.windows.net/powerbi/api/.default";
    internal const string AzureResourceUrl = "https://management.azure.com//.default";

    public abstract TokenCredential GetTokenCredential();
    
    public abstract TokenCredential GetTokenServiceCredential(ITokenService tokenService);
    
    public DatasetClient CreateDatasetClient(ITokenService tokenService) => new(this, tokenService);

    public abstract Task TestConnection();

    public abstract Task TestPowerBiConnection();
}