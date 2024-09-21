using Biflow.Core.Entities;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class QlikCloudClientDeserializeTests
{
    private static readonly QlikCloudClient client = CreateClient();

    [Fact]
    public void QlikCloudClientId_NotEmptyGuid() => Assert.NotEqual(client.QlikCloudClientId, Guid.Empty);

    [Fact]
    public void ApiToken_Empty() => Assert.Empty(client.ApiToken);

    private static QlikCloudClient CreateClient()
    {
        var client = new QlikCloudClient
        {
            QlikCloudClientName = "Test",
            EnvironmentUrl = "my_env_url",
            ApiToken = "api_token"
        };
        client.SetPrivatePropertyValue("QlikCloudClientId", Guid.NewGuid());
        return client.JsonRoundtrip(EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
    }
}
