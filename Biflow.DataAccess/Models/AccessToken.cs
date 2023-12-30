using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biflow.DataAccess.Models;

[Table("AccessToken")]
[PrimaryKey("AppRegistrationId", "ResourceUrl")]
public class AccessToken(Guid appRegistrationId, string resourceUrl, string token, DateTimeOffset expiresOn)
{
    public Guid AppRegistrationId { get; set; } = appRegistrationId;

    public AppRegistration AppRegistration { get; set; } = null!;

    [Unicode(false)]
    [MaxLength(1000)]
    public string ResourceUrl { get; set; } = resourceUrl;

    public string Token { get; set; } = token;

    public DateTimeOffset ExpiresOn { get; set; } = expiresOn;

}
