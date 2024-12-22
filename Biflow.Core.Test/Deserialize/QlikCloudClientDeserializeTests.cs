using Biflow.Core.Entities;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class QlikCloudClientDeserializeTests
{
    private readonly QlikCloudEnvironment _client = CreateClient();

    [Fact]
    public void QlikCloudEnvironmentId_NotEmptyGuid() => Assert.NotEqual(_client.QlikCloudEnvironmentId, Guid.Empty);

    [Fact]
    public void ApiToken_Empty() => Assert.Empty(_client.ApiToken);

    private static QlikCloudEnvironment CreateClient()
    {
        var client = new QlikCloudEnvironment
        {
            QlikCloudEnvironmentId = Guid.NewGuid(),
            QlikCloudEnvironmentName = "Test",
            EnvironmentUrl = "my_env_url",
            ApiToken = "api_token"
        };
        return client.JsonRoundtrip(EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
    }
}
