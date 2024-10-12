using Biflow.Core.Entities;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class CredentialDeserializeTests
{
    private static readonly Credential credential = CreateCredential();

    [Fact]
    public void CredentialId_NotEmptyGuid() => Assert.NotEqual(credential.CredentialId, Guid.Empty);

    [Fact]
    public void Password_Empty() => Assert.Empty(credential.Password ?? "");

    private static Credential CreateCredential()
    {
        var credential = new Credential
        {
            Domain = ".",
            Username = "test",
            Password = "myPassword9000"
        };
        credential.SetPrivatePropertyValue("CredentialId", Guid.NewGuid());
        return credential.JsonRoundtrip(EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
    }
}
