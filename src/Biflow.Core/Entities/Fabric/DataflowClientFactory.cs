using Biflow.Core.Interfaces;

namespace Biflow.Core.Entities;

public class DataflowClientFactory(ITokenService tokenService, IHttpClientFactory httpClientFactory)
{
    public DataflowClient Create(AzureCredential azureCredential) =>
        new(azureCredential, tokenService, httpClientFactory);
}