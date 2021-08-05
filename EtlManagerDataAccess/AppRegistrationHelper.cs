using EtlManagerDataAccess.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlManagerDataAccess
{
    public class AppRegistrationHelper
    {
        private const string AuthenticationUrl = "https://login.microsoftonline.com/";
        private const string ResourceUrl = "https://management.azure.com/";

        public static async Task TestConnection(AppRegistration appRegistration)
        {
            var context = new AuthenticationContext(AuthenticationUrl + appRegistration.TenantId);
            var clientCredential = new ClientCredential(appRegistration.ClientId, appRegistration.ClientSecret);
            var _ = await context.AcquireTokenAsync(ResourceUrl, clientCredential);
        }
    }
}
