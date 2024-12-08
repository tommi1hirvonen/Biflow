using Biflow.Core.Entities;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class AzureCredentialDeserializeTests
{
    private readonly ServicePrincipalCredential azureCredential = CreateCredential();

    [Fact]
    public void AzureCredentialId_NotEmptyGuid() => Assert.NotEqual(azureCredential.AzureCredentialId, Guid.Empty);

    [Fact]
    public void ClientSecret_Empty() => Assert.Empty(azureCredential.ClientSecret ?? "");

    private static ServicePrincipalCredential CreateCredential()
    {
        var credential = new ServicePrincipalCredential
        {
            AzureCredentialName = "Test name",
            TenantId = "test_tenant_id",
            ClientId = "test_client_id",
            ClientSecret = "test_client_secret"
        };
        credential.SetPrivatePropertyValue("AzureCredentialId", Guid.NewGuid());
        return credential.JsonRoundtrip(EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
    }
}
