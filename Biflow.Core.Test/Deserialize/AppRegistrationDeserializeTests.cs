using Biflow.Core.Entities;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class AppRegistrationDeserializeTests
{
    private readonly AppRegistration appRegistration = CreateAppRegistration();

    [Fact]
    public void AppRegistrationId_NotEmptyGuid() => Assert.NotEqual(appRegistration.AppRegistrationId, Guid.Empty);

    [Fact]
    public void ClientSecret_Empty() => Assert.Empty(appRegistration.ClientSecret ?? "");

    private static AppRegistration CreateAppRegistration()
    {
        var appRegistration = new AppRegistration
        {
            AppRegistrationName = "Test name",
            TenantId = "test_tenant_id",
            ClientId = "test_client_id",
            ClientSecret = "test_client_secret"
        };
        appRegistration.SetPrivatePropertyValue("AppRegistrationId", Guid.NewGuid());
        return appRegistration.JsonRoundtrip(EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
    }
}
