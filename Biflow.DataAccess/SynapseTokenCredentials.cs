using Azure.Core;
using Biflow.DataAccess.Models;

namespace Biflow.DataAccess;

internal class SynapseTokenCredential(ITokenService tokenService, AppRegistration appRegistration) : TokenCredential
{
    private readonly ITokenService _tokenService = tokenService;
    private readonly AppRegistration _appRegistration = appRegistration;

    private const string ResourceUrl = "https://dev.azuresynapse.net//.default";

    public override Azure.Core.AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        var (token, expiresOn) = _tokenService.GetTokenAsync(_appRegistration, ResourceUrl).Result;
        return new Azure.Core.AccessToken(token, expiresOn);
    }

    public override async ValueTask<Azure.Core.AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        var (token, expiresOn) = await _tokenService.GetTokenAsync(_appRegistration, ResourceUrl);
        return new Azure.Core.AccessToken(token, expiresOn);
    }
}
