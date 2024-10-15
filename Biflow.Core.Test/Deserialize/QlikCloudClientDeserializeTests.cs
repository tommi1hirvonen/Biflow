using Biflow.Core.Entities;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class QlikCloudClientDeserializeTests
{
    private static readonly QlikCloudEnvironment client = CreateClient();

    [Fact]
    public void QlikCloudEnvironmentId_NotEmptyGuid() => Assert.NotEqual(client.QlikCloudEnvironmentId, Guid.Empty);

    [Fact]
    public void ApiToken_Empty() => Assert.Empty(client.ApiToken);

    private static QlikCloudEnvironment CreateClient()
    {
        var client = new QlikCloudEnvironment
        {
            QlikCloudEnvironmentName = "Test",
            EnvironmentUrl = "my_env_url",
            ApiToken = "api_token"
        };
        client.SetPrivatePropertyValue("QlikCloudEnvironmentId", Guid.NewGuid());
        return client.JsonRoundtrip(EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
    }
}
