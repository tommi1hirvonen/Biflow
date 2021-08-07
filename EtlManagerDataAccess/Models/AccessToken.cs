using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace EtlManagerDataAccess.Models
{
    public class AccessToken
    {
        public AccessToken(Guid appRegistrationId, string resourceUrl, string token, DateTime expiresOn)
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

        public DateTime ExpiresOn { get; set; }

    }
}
