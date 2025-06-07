using Biflow.Core.Entities;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class CredentialDeserializeTests
{
    private readonly Credential _credential = CreateCredential();

    [Fact]
    public void CredentialId_NotEmptyGuid() => Assert.NotEqual(_credential.CredentialId, Guid.Empty);

    [Fact]
    public void Password_Empty() => Assert.Empty(_credential.Password ?? "");

    private static Credential CreateCredential()
    {
        var credential = new Credential
        {
            CredentialId = Guid.NewGuid(),
            Domain = ".",
            Username = "test",
            Password = "myPassword9000"
        };
        return credential.JsonRoundtrip(EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
    }
}
