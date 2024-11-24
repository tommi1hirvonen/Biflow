using Azure.Core;
using Biflow.Core.Interfaces;

namespace Biflow.Core.Entities;

internal class AzureTokenCredential(
    ITokenService tokenService,
    AppRegistration appRegistration,
    string resourceUrl)
    : TokenCredential
{
    public override Azure.Core.AccessToken GetToken(
        TokenRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        var (token, expiresOn) = tokenService
            .GetTokenAsync(appRegistration, resourceUrl, cancellationToken)
            .GetAwaiter()
            .GetResult();
        return new Azure.Core.AccessToken(token, expiresOn);
    }

    public override async ValueTask<Azure.Core.AccessToken> GetTokenAsync(
        TokenRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        var (token, expiresOn) = await tokenService.GetTokenAsync(appRegistration, resourceUrl, cancellationToken);
        return new Azure.Core.AccessToken(token, expiresOn);
    }
}
