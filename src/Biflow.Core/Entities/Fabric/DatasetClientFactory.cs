using Biflow.Core.Interfaces;

namespace Biflow.Core.Entities;

public class DatasetClientFactory(ITokenService tokenService)
{
    public DatasetClient Create(AzureCredential azureCredential) =>
        new(azureCredential, tokenService);
}