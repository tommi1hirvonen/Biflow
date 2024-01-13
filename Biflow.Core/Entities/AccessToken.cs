using System.ComponentModel.DataAnnotations;

namespace Biflow.Core.Entities;

public class AccessToken(Guid appRegistrationId, string resourceUrl, string token, DateTimeOffset expiresOn)
{
    public Guid AppRegistrationId { get; set; } = appRegistrationId;

    public AppRegistration AppRegistration { get; set; } = null!;

    [MaxLength(1000)]
    public string ResourceUrl { get; set; } = resourceUrl;

    public string Token { get; set; } = token;

    public DateTimeOffset ExpiresOn { get; set; } = expiresOn;

}
