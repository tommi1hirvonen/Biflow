using Biflow.Core.Entities;
using Xunit;

namespace Biflow.Core.Test.Deserialize;

public class ConnectionDeserializeTests
{
    private static readonly MsSqlConnection connectionWithPassword = CreateConnection("user=mySqlLogin;password=myPassword9000;");
    private static readonly MsSqlConnection connectionNoPassword = CreateConnection("user=mySqlLogin;authentication=integrated");

    [Fact]
    public void Connection_ConnectionId_NotEmpty()
    {
        Assert.NotEqual(connectionNoPassword.ConnectionId, Guid.Empty);
    }

    [Fact]
    public void Connection_CredentialId_EqualCredentialCredentialId()
    {
        Assert.Equal(connectionNoPassword.CredentialId, connectionNoPassword.Credential!.CredentialId);
    }

    [Fact]
    public void Connection_PasswordConnectionString_Empty()
    {
        Assert.Empty(connectionWithPassword.ConnectionString);
    }

    [Fact]
    public void Connection_NoPasswordConnectionString_NotEmpty()
    {
        Assert.NotEmpty(connectionNoPassword.ConnectionString);
    }

    [Fact]
    public void Connection_NoPasswordConnectionString_DoesNotContainPassword()
    {
        Assert.DoesNotContain("password", connectionNoPassword.ConnectionString, StringComparison.OrdinalIgnoreCase);
    }

    private static MsSqlConnection CreateConnection(string connectionString)
    {
        var credentialId = Guid.NewGuid();
        var credential = new Credential
        {
            Domain = ".",
            Username = "test",
            Password = "myPassword9000"
        };
        credential.SetPrivatePropertyValue("CredentialId", credentialId);
        var connection = new MsSqlConnection
        {
            ConnectionName = "Test connection",
            ConnectionString = connectionString,
            Credential = credential,
            CredentialId = credentialId
        };
        connection.SetPrivatePropertyValue("ConnectionId", Guid.NewGuid(), typeof(ConnectionBase));
        return connection.JsonRoundtrip(EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
    }
}
