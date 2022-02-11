namespace Biflow.DataAccess.Models;

public class AccessToken
{
    public AccessToken(Guid appRegistrationId, string resourceUrl, string token, DateTimeOffset expiresOn)
    {
        AppRegistrationId = appRegistrationId;
        ResourceUrl = resourceUrl;
        Token = token;
        ExpiresOn = expiresOn;
    }

    public Guid AppRegistrationId { get; set; }

    public AppRegistration AppRegistration { get; set; } = null!;

    public string ResourceUrl { get; set; }

    public string Token { get; set; }

    public DateTimeOffset ExpiresOn { get; set; }

}
