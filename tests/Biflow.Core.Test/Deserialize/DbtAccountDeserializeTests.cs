using Biflow.Core.Entities;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class DbtAccountDeserializeTests
{
    private static readonly DbtAccount Account = CreateClient();

    [Fact]
    public void QlikCloudEnvironmentId_NotEmptyGuid() => Assert.NotEqual(Account.DbtAccountId, Guid.Empty);

    [Fact]
    public void ApiToken_Empty() => Assert.Empty(Account.ApiToken);

    private static DbtAccount CreateClient()
    {
        var account = new DbtAccount
        {
            DbtAccountId = Guid.NewGuid(),
            DbtAccountName = "Test",
            ApiBaseUrl = "https://api.test.test",
            AccountId = "0123456789",
            ApiToken = "api_token"
        };
        return account.JsonRoundtrip(EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
    }
}