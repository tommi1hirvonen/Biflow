using Biflow.Core.Interfaces;

namespace Biflow.Core.Entities;

public class FabricWorkspaceClientFactory(IHttpClientFactory httpClientFactory, ITokenService tokenService)
{
    public FabricWorkspaceClient Create(AzureCredential azureCredential) =>
        new(azureCredential, tokenService, httpClientFactory);
}