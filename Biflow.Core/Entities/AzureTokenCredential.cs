using Azure.Core;
using Biflow.Core.Interfaces;

namespace Biflow.Core.Entities;

internal class ServicePrincipalAzureTokenCredential(
    ITokenService tokenService, ServicePrincipalCredential azureCredential) : TokenCredential
{
    public override Azure.Core.AccessToken GetToken(
        TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        var (token, expiresOn) = tokenService
            .GetTokenAsync(azureCredential, requestContext.Scopes, cancellationToken)
            .GetAwaiter()
            .GetResult();
        return new Azure.Core.AccessToken(token, expiresOn);
    }

    public override async ValueTask<Azure.Core.AccessToken> GetTokenAsync(
        TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        var (token, expiresOn) = await tokenService
            .GetTokenAsync(azureCredential, requestContext.Scopes, cancellationToken);
        return new Azure.Core.AccessToken(token, expiresOn);
    }
}

internal class UsernamePasswordAzureTokenCredential(
    ITokenService tokenService, OrganizationalAccountCredential azureCredential) : TokenCredential
{
    public override Azure.Core.AccessToken GetToken(
        TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        var (token, expiresOn) = tokenService
            .GetTokenAsync(azureCredential, requestContext.Scopes, cancellationToken)
            .GetAwaiter()
            .GetResult();
        return new Azure.Core.AccessToken(token, expiresOn);
    }

    public override async ValueTask<Azure.Core.AccessToken> GetTokenAsync(
        TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        var (token, expiresOn) = await tokenService
            .GetTokenAsync(azureCredential, requestContext.Scopes, cancellationToken);
        return new Azure.Core.AccessToken(token, expiresOn);
    }
}
