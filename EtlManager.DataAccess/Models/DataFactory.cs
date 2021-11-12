using Microsoft.Azure.Management.DataFactory;
using Microsoft.Azure.Management.DataFactory.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Microsoft.Rest.Azure;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EtlManager.DataAccess.Models;

public class DataFactory
{
    [Key]
    [Required]
    [Display(Name = "Data Factory id")]
    public Guid DataFactoryId { get; set; }

    [Required]
    [Display(Name = "Data Factory name")]
    public string? DataFactoryName { get; set; }

    [Required]
    [Display(Name = "Subscription id")]
    [MaxLength(36)]
    [MinLength(36)]
    public string? SubscriptionId { get; set; }


    [Required]
    [Display(Name = "Resource group name")]
    public string? ResourceGroupName { get; set; }

    [Required]
    [Display(Name = "Resource name")]
    public string? ResourceName { get; set; }

    [Required]
    [Display(Name = "App registration")]
    public Guid? AppRegistrationId { get; set; }

    public AppRegistration AppRegistration { get; set; } = null!;

    private const string AuthenticationUrl = "https://login.microsoftonline.com/";
    private const string ResourceUrl = "https://management.azure.com/";

    private async Task<DataFactoryManagementClient> GetClientAsync(ITokenService tokenService)
    {
        var accessToken = await tokenService.GetTokenAsync(AppRegistration, ResourceUrl);
        var credentials = new TokenCredentials(accessToken);
        return new DataFactoryManagementClient(credentials) { SubscriptionId = SubscriptionId };
    }

    public async Task<string> StartPipelineRunAsync(ITokenService tokenService, string pipelineName, IDictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        var client = await GetClientAsync(tokenService);
        var createRunResponse = await client.Pipelines.CreateRunAsync(ResourceGroupName, ResourceName, pipelineName,
            parameters: parameters, cancellationToken: cancellationToken);
        return createRunResponse.RunId;
    }

    public async Task<PipelineRun> GetPipelineRunAsync(ITokenService tokenService, string runId, CancellationToken cancellationToken)
    {
        var client = await GetClientAsync(tokenService);
        return await client.PipelineRuns.GetAsync(ResourceGroupName, ResourceName, runId, cancellationToken);
    }

    public async Task CancelPipelineRunAsync(ITokenService tokenService, string runId)
    {
        var client = await GetClientAsync(tokenService);
        await client.PipelineRuns.CancelAsync(ResourceGroupName, ResourceName, runId, isRecursive: true);
    }

    public async Task<Dictionary<string, List<string>>> GetPipelinesAsync(ITokenService tokenService)
    {
        var client = await GetClientAsync(tokenService);
        var allPipelines = new List<IPage<PipelineResource>>();

        var pipelines = await client.Pipelines.ListByFactoryAsync(ResourceGroupName, ResourceName);
        allPipelines.Add(pipelines);
        var nextPage = pipelines.NextPageLink;

        while (nextPage is not null)
        {
            var pipelines_ = await client.Pipelines.ListByFactoryNextAsync(nextPage);
            allPipelines.Add(pipelines_);
            nextPage = pipelines_.NextPageLink;
        }

        // Key = Folder
        // Value = List of pipelines in that folder
        return allPipelines
            .SelectMany(p => p.Select(p_ => (Folder: p_.Folder?.Name ?? "/", p_.Name))) // Replace null folder (root) with forward slash.
            .GroupBy(p => p.Folder)
            .ToDictionary(p => p.Key, p => p.Select(p => p.Name).ToList());
    }

    public async Task TestConnection(AppRegistration appRegistration)
    {
        var context = new AuthenticationContext(AuthenticationUrl + appRegistration.TenantId);
        var clientCredential = new ClientCredential(appRegistration.ClientId, appRegistration.ClientSecret);
        var result = await context.AcquireTokenAsync(ResourceUrl, clientCredential);
        var credentials = new TokenCredentials(result.AccessToken);
        var client = new DataFactoryManagementClient(credentials) { SubscriptionId = SubscriptionId };
        var _ = await client.Factories.GetAsync(ResourceGroupName, ResourceName);
    }
}
