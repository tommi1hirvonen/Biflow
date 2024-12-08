using Biflow.Core.Entities;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class BlobStorageClientDeserializeTests
{
    private static readonly BlobStorageClient azureCredentialClient = CreateAzureCredentialClient();
    private static readonly BlobStorageClient connectionStringClient = CreateConnectionStringClient();
    private static readonly BlobStorageClient sensitiveUrlClient = CreateSensitiveUrlClient();
    private static readonly BlobStorageClient nonSensitiveUrlClient = CreateNonSensitiveUrlClient();

    [Fact]
    public void AzureCredentialClient_AzureCredentialId_NotEmptyGuid() => Assert.NotEqual(azureCredentialClient.AzureCredentialId, Guid.Empty);

    [Fact]
    public void AzureCredentialClient_Url_NotEmpty() => Assert.NotEmpty(azureCredentialClient.StorageAccountUrl ?? "");

    [Fact]
    public void ConnectionStringClient_ConnectionString_Empty() => Assert.Empty(connectionStringClient.ConnectionString ?? "");

    [Fact]
    public void SensitiveUrlClient_Url_Empty() => Assert.Empty(sensitiveUrlClient.StorageAccountUrl ?? "");

    [Fact]
    public void NonSensitiveUrlClient_Url_NotEmpty() => Assert.NotEmpty(nonSensitiveUrlClient.StorageAccountUrl ?? "");

    private static BlobStorageClient CreateAzureCredentialClient()
    {
        var credential = new ServicePrincipalCredential
        {
            AzureCredentialName = "Test"
        };
        credential.SetPrivatePropertyValue("AzureCredentialId", Guid.NewGuid());
        var client = new BlobStorageClient
        {
            BlobStorageClientName = "Test"
        };
        client.UseCredential(credential, "client_url.com");
        return client.JsonRoundtrip(EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
    }

    private static BlobStorageClient CreateConnectionStringClient()
    {
        var client = new BlobStorageClient
        {
            BlobStorageClientName = "Test"
        };
        client.UseConnectionString("connection_string");
        return client.JsonRoundtrip(EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
    }

    private static BlobStorageClient CreateSensitiveUrlClient()
    {
        var client = new BlobStorageClient
        {
            BlobStorageClientName = "Test"
        };
        client.UseUrl("client_url.com?param1=value&sig=signatureValue");
        return client.JsonRoundtrip(EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
    }

    private static BlobStorageClient CreateNonSensitiveUrlClient()
    {
        var client = new BlobStorageClient
        {
            BlobStorageClientName = "Test"
        };
        client.UseUrl("client_url.com?param1=value");
        return client.JsonRoundtrip(EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
    }
}
