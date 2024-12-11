using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Azure.Core;
using Microsoft.PowerBI.Api;
using Microsoft.Rest;

namespace Biflow.Core.Entities;

[JsonDerivedType(typeof(ServicePrincipalAzureCredential), nameof(AzureCredentialType.ServicePrincipal))]
[JsonDerivedType(typeof(OrganizationalAccountAzureCredential), nameof(AzureCredentialType.OrganizationalAccount))]
[JsonDerivedType(typeof(ManagedIdentityAzureCredential), nameof(AzureCredentialType.ManagedIdentity))]
public abstract class AzureCredential(AzureCredentialType azureCredentialType)
{
    [Required]
    [JsonInclude]
    public Guid AzureCredentialId { get; private set; }

    [Required]
    [MaxLength(250)]
    public string? AzureCredentialName { get; set; }
    
    public AzureCredentialType AzureCredentialType { get; init; } = azureCredentialType;

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
    internal const string FabricResourceUrl = "https://api.fabric.microsoft.com//.default";

    public abstract TokenCredential GetTokenCredential();
    
    public TokenCredential GetTokenServiceCredential(ITokenService tokenService) =>
        new AzureTokenCredential(tokenService, this);
    
    public DatasetClient CreateDatasetClient(ITokenService tokenService) => new(this, tokenService);
    
    public DataflowClient CreateDataflowClient(ITokenService tokenService, IHttpClientFactory httpClientFactory) =>
        new(this, tokenService, httpClientFactory);
    
    public FabricWorkspaceClient CreateFabricWorkspaceClient(ITokenService tokenService) => new(this, tokenService);

    public async Task TestConnection(CancellationToken cancellationToken = default)
    {
        var credential = GetTokenCredential();
        var context = new TokenRequestContext([AzureResourceUrl]);
        _ = await credential.GetTokenAsync(context, cancellationToken);
    }

    public async Task TestPowerBiConnection(CancellationToken cancellationToken = default)
    {
        var credential = GetTokenCredential();
        var context = new TokenRequestContext([PowerBiResourceUrl]);
        var token = await credential.GetTokenAsync(context, CancellationToken.None);
        var credentials = new TokenCredentials(token.Token);
        var client = new PowerBIClient(credentials);
        _ = await client.Groups.GetGroupsAsync(cancellationToken: cancellationToken);
    }
}