using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

namespace Biflow.Ui.Authentication;

internal static class AuthExtensions
{
    /// <summary>
    /// Adds authentication services based on settings defined in configuration. Needs to be called after AddUiCoreServices().
    /// </summary>
    /// <param name="services"/>
    /// <param name="configuration">Top level configuration object</param>
    /// <returns>The IServiceCollection passed as parameter</returns>
    /// <exception cref="ArgumentException">Thrown if an incorrect configuration is detected</exception>
    public static IServiceCollection AddUiAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var authentication = configuration.GetValue<string>("Authentication");
        switch (authentication)
        {
            case "BuiltIn":
                services.AddScoped<IAuthHandler, BuiltInAuthHandler>();
                services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(options =>
                    {
                        options.LoginPath = "/login";
                        options.ReturnUrlParameter = "redirectUrl";
                    });
                break;
            case "Windows":
                services.AddMemoryCache();
                services.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();
                services.AddAuthorizationBuilder()
                    .SetFallbackPolicy(new AuthorizationPolicyBuilder().AddRequirements(new UserExistsRequirement()).Build());
                services.AddScoped<IAuthorizationHandler, WindowsAuthorizationHandler>();
                services.AddScoped<IClaimsTransformation, ClaimsTransformer>();
                break;
            case "AzureAd":
                services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApp(configuration.GetSection("AzureAd"));
                services.AddControllersWithViews().AddMicrosoftIdentityUI();
                services.AddAuthorization(options =>
                {
                    options.FallbackPolicy = options.DefaultPolicy;
                });
                services.AddScoped<IClaimsTransformation, ClaimsTransformer>();
                break;
            case "Ldap":
                services.AddScoped<IAuthHandler, LdapAuthHandler>();
                services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(options =>
                    {
                        options.LoginPath = "/login";
                        options.ReturnUrlParameter = "redirectUrl";
                    });
                break;
            default:
                throw new ArgumentException($"Invalid Authentication setting: {authentication}");
        }
        return services;
    }
}