using Microsoft.Extensions.Configuration;
using System.DirectoryServices.Protocols;
using System.Security.Authentication;

namespace Biflow.Ui.Core;

internal class LdapAuthHandler(IConfiguration configuration, UserService users) : IAuthHandler
{
    private readonly IConfiguration _configuration = configuration;
    private readonly UserService _users = users;
    private readonly string[] _attributesToQuery = new string[] { "userPrincipalName" };

    public async Task<IEnumerable<string>> AuthenticateUserInternalAsync(string username, string password)
    {
        var section = _configuration.GetSection("Ldap");
        var server = section.GetValue<string>("Server");
        var port = section.GetValue<int>("Port");
        var useSsl = section.GetValue<bool>("UseSsl");
        var userstore = section.GetValue<string>("UserStoreDistinguishedName");

        ArgumentNullException.ThrowIfNull(server);
        if (port <= 0)
        {
            throw new ArgumentException($"Invalid port for LDAP server: {port}");
        }
        ArgumentNullException.ThrowIfNull(userstore);

        var ldap = new LdapDirectoryIdentifier(server, port);
        using var connection = new LdapConnection(ldap)
        {
            AuthType = AuthType.Basic,
            Credential = new(username, password)
        };
        connection.SessionOptions.ProtocolVersion = 3;
        connection.SessionOptions.SecureSocketLayer = useSsl;
        try
        {
            connection.Bind();
        }
        catch (LdapException ex)
        {
            if (ex.ErrorCode == 49)
            {
                throw new InvalidCredentialException(ex.Message);
            }
            throw;
        }

        var request = new SearchRequest(
            userstore,
            $"(&(objectClass=user)(userPrincipalName={username}))",
            SearchScope.Subtree,
            _attributesToQuery);

        var response = (SearchResponse)connection.SendRequest(request);
        var results = response.Entries.Cast<SearchResultEntry>();
        if (!results.Any())
        {
            return Enumerable.Empty<string>();
        }

        var roles = await _users.GetUserRolesAsync(username);
        if (!roles.Any())
        {
            throw new AuthenticationException("User is not authorized to access this application");
        }

        return roles;
    }
}
