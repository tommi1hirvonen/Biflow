using Biflow.Core.Entities;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class FunctionAppDeserializeTests
{
    private static readonly FunctionApp functionApp = CreateFunctionApp();

    [Fact]
    public void FunctionAppId_NotEmptyGuid() => Assert.NotEqual(functionApp.FunctionAppId, Guid.Empty);

    [Fact]
    public void FunctionAppKey_Empty() => Assert.Empty(functionApp.FunctionAppKey ?? "");

    [Fact]
    public void AppRegistrationId_NotEmptyGuid() => Assert.NotEqual(functionApp.AppRegistrationId, Guid.Empty);

    [Fact]
    public void AppRegistration_Null() => Assert.Null(functionApp.AppRegistration);

    private static FunctionApp CreateFunctionApp()
    {
        var appRegistration = new AppRegistration();
        var appRegistrationId = Guid.NewGuid();
        appRegistration.SetPrivatePropertyValue("AppRegistrationId", appRegistrationId);
        var functionApp = new FunctionApp
        {
            FunctionAppName = "test",
            SubscriptionId = "subscription_id",
            ResourceGroupName = "rg_name",
            ResourceName = "resource_name",
            FunctionAppKey = "app_key",
            AppRegistration = appRegistration
        };
        functionApp.SetPrivatePropertyValue("FunctionAppId", Guid.NewGuid());
        return functionApp.JsonRoundtrip(EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
    }
}
