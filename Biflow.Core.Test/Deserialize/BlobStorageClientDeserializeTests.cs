using Biflow.Core.Entities;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class BlobStorageClientDeserializeTests
{
    private static readonly BlobStorageClient appRegistrationClient = CreateAppRegistrationClient();
    private static readonly BlobStorageClient connectionStringClient = CreateConnectionStringClient();
    private static readonly BlobStorageClient sensitiveUrlClient = CreateSensitiveUrlClient();
    private static readonly BlobStorageClient nonSensitiveUrlClient = CreateNonSensitiveUrlClient();

    [Fact]
    public void AppRegistrationClient_AppRegistrationId_NotEmptyGuid() => Assert.NotEqual(appRegistrationClient.AppRegistrationId, Guid.Empty);

    [Fact]
    public void AppRegistrationClient_Url_NotEmpty() => Assert.NotEmpty(appRegistrationClient.StorageAccountUrl ?? "");

    [Fact]
    public void ConnectionStringClient_ConnectionString_Empty() => Assert.Empty(connectionStringClient.ConnectionString ?? "");

    [Fact]
    public void SensitiveUrlClient_Url_Empty() => Assert.Empty(sensitiveUrlClient.StorageAccountUrl ?? "");

    [Fact]
    public void NonSensitiveUrlClient_Url_NotEmpty() => Assert.NotEmpty(nonSensitiveUrlClient.StorageAccountUrl ?? "");

    private static BlobStorageClient CreateAppRegistrationClient()
    {
        var appRegistration = new AppRegistration
        {
            AppRegistrationName = "Test"
        };
        appRegistration.SetPrivatePropertyValue("AppRegistrationId", Guid.NewGuid());
        var client = new BlobStorageClient
        {
            BlobStorageClientName = "Test"
        };
        client.UseAppRegistration(appRegistration, "client_url.com");
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
