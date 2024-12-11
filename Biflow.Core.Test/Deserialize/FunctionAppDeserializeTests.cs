using Biflow.Core.Entities;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class FunctionAppDeserializeTests
{
    private static readonly FunctionApp FunctionApp = CreateFunctionApp();

    [Fact]
    public void FunctionAppId_NotEmptyGuid() => Assert.NotEqual(FunctionApp.FunctionAppId, Guid.Empty);

    [Fact]
    public void FunctionAppKey_Empty() => Assert.Empty(FunctionApp.FunctionAppKey ?? "");

    [Fact]
    public void AzureCredentialId_NotEmptyGuid() => Assert.NotEqual(FunctionApp.AzureCredentialId, Guid.Empty);

    [Fact]
    public void AzureCredential_Null() => Assert.Null(FunctionApp.AzureCredential);

    private static FunctionApp CreateFunctionApp()
    {
        var credential = new ServicePrincipalAzureCredential();
        var credentialId = Guid.NewGuid();
        credential.SetPrivatePropertyValue("AzureCredentialId", credentialId);
        var functionApp = new FunctionApp
        {
            FunctionAppName = "test",
            SubscriptionId = "subscription_id",
            ResourceGroupName = "rg_name",
            ResourceName = "resource_name",
            FunctionAppKey = "app_key",
            AzureCredential = credential
        };
        functionApp.SetPrivatePropertyValue("FunctionAppId", Guid.NewGuid());
        return functionApp.JsonRoundtrip(EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
    }
}
