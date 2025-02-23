using Azure.ResourceManager;
using Azure.ResourceManager.DataFactory;
using Biflow.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Entities;

public class DataFactory() : PipelineClient(PipelineClientType.DataFactory)
{
    [Required]
    [MaxLength(36)]
    [MinLength(36)]
    public string SubscriptionId { get; set; } = "";

    [Required]
    [MaxLength(250)]
    public string ResourceGroupName { get; set; } = "";

    [Required]
    [MaxLength(250)]
    public string ResourceName { get; set; } = "";

    internal const string ResourceUrl = "https://management.azure.com//.default";

    public override IPipelineClient CreatePipelineClient(ITokenService tokenService) => new DataFactoryClient(this, tokenService);

    public async Task TestConnection(AzureCredential azureCredential)
    {
        var credential = azureCredential.GetTokenCredential();
        var client = new ArmClient(credential);
        var dataFactory = GetDataFactoryResource(client);
        _ = await dataFactory.GetAsync();
    }

    private DataFactoryResource GetDataFactoryResource(ArmClient client)
    {
        var identifier = DataFactoryResource.CreateResourceIdentifier(SubscriptionId, ResourceGroupName, ResourceName);
        var resource = client.GetDataFactoryResource(identifier);
        return resource;
    }
}
