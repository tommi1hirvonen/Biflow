using Azure.Core;
using Biflow.Core.Interfaces;

namespace Biflow.Core.Entities;

internal class AzureTokenCredential(ITokenService tokenService, AppRegistration appRegistration, string resourceUrl) : TokenCredential
{
    private readonly ITokenService _tokenService = tokenService;
    private readonly AppRegistration _appRegistration = appRegistration;
    private readonly string resourceUrl = resourceUrl;

    public override Azure.Core.AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        var (token, expiresOn) = _tokenService.GetTokenAsync(_appRegistration, resourceUrl).GetAwaiter().GetResult();
        return new Azure.Core.AccessToken(token, expiresOn);
    }

    public override async ValueTask<Azure.Core.AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        var (token, expiresOn) = await _tokenService.GetTokenAsync(_appRegistration, resourceUrl);
        return new Azure.Core.AccessToken(token, expiresOn);
    }
}
