using Azure.Identity;
using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using SynapsePipelineClient = Azure.Analytics.Synapse.Artifacts.PipelineClient;

namespace Biflow.Core.Entities;

public class SynapseWorkspace() : PipelineClient(PipelineClientType.Synapse)
{
    [Required]
    [MaxLength(500)]
    public string SynapseWorkspaceUrl { get; set; } = "";

    internal Uri SynapseEndpoint => new(SynapseWorkspaceUrl);

    internal const string ResourceUrl = "https://dev.azuresynapse.net//.default";

    public override IPipelineClient CreatePipelineClient(ITokenService tokenService) => new SynapseWorkspaceClient(this, tokenService);

    public async Task TestConnection(AppRegistration appRegistration)
    {
        var token = new ClientSecretCredential(appRegistration.TenantId, appRegistration.ClientId, appRegistration.ClientSecret);
        var pipelineClient = new SynapsePipelineClient(SynapseEndpoint, token);
        var pageable = pipelineClient.GetPipelinesByWorkspaceAsync();
        await foreach (var _ in pageable)
        {
        }
    }
}