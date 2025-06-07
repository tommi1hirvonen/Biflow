using Biflow.Core.Entities;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class AzureCredentialDeserializeTests
{
    private readonly ServicePrincipalAzureCredential _azureAzureCredential = CreateCredential();

    [Fact]
    public void AzureCredentialId_NotEmptyGuid() => Assert.NotEqual(_azureAzureCredential.AzureCredentialId, Guid.Empty);

    [Fact]
    public void ClientSecret_Empty() => Assert.Empty(_azureAzureCredential.ClientSecret ?? "");

    private static ServicePrincipalAzureCredential CreateCredential()
    {
        var credential = new ServicePrincipalAzureCredential
        {
            AzureCredentialId = Guid.NewGuid(),
            AzureCredentialName = "Test name",
            TenantId = "test_tenant_id",
            ClientId = "test_client_id",
            ClientSecret = "test_client_secret"
        };
        return credential.JsonRoundtrip(EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
    }
}
