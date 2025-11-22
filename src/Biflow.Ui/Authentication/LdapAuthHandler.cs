using System.DirectoryServices.Protocols;
using System.Net;
using System.Security.Authentication;
using Biflow.Ui.Mediator.Queries.User;

namespace Biflow.Ui.Authentication;

internal class LdapAuthHandler(IConfiguration configuration, IMediator mediator) : IAuthHandler
{
    private readonly string[] _attributesToQuery = ["userPrincipalName"];

    public async Task<IReadOnlyList<string>> AuthenticateUserInternalAsync(string username, string password)
    {
        var section = configuration.GetSection("Ldap");
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
        using var connection = new LdapConnection(ldap);
        connection.AuthType = AuthType.Basic;
        connection.Credential = new NetworkCredential(username, password);
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

        var searchResponse = (SearchResponse)connection.SendRequest(request);
        var results = searchResponse.Entries.Cast<SearchResultEntry>();
        if (!results.Any())
        {
            return [];
        }

        var rolesResponse = await mediator.SendAsync(new UserRolesQuery(username));
        return !rolesResponse.Roles.Any()
            ? throw new AuthenticationException("User is not authorized to access this application")
            : rolesResponse.Roles;
    }
}
