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
            CredentialId = credentialId,
            Domain = ".",
            Username = "test",
            Password = "myPassword9000"
        };
        var connection = new MsSqlConnection
        {
            ConnectionId = Guid.NewGuid(),
            ConnectionName = "Test connection",
            ConnectionString = connectionString,
            Credential = credential,
            CredentialId = credentialId
        };
        return connection.JsonRoundtrip(EnvironmentSnapshot.JsonSerializerOptionsPreserveReferences);
    }
}
