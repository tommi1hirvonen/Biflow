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
    /// Adds authentication services based on settings defined in configurations.
    /// Needs to be called after AddUiCoreServices().
    /// </summary>
    /// <param name="services"/>
    /// <param name="configuration">Top level configuration object</param>
    /// <returns>The IServiceCollection passed as the parameter</returns>
    /// <exception cref="ArgumentException">Thrown if an incorrect configuration is detected</exception>
    public static IServiceCollection AddUiAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var authentication = configuration.GetValue<string>("Authentication");
        services.AddMemoryCache();
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
                // With BuiltIn authentication, a user is de facto authorized if they are authenticated.
                // Only require authentication for the global policy.
                services.AddAuthorizationBuilder()
                    .AddPolicy(AuthConstants.GlobalAuthPolicy, policy => policy.RequireAuthenticatedUser());
                // Do not set a fallback policy,
                // as that will prevent CSS and other static files from being served on the login page.
                
                // With BuiltIn authentication, role claims are being added in Login.razor
                // => no need to add claims transformation.
                break;
            case "Windows":
                services.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();
                // With Windows authentication, a user can be authenticated but not authorized.
                // For both the global and fallback policies, we require that the user exists
                // (i.e., has at least one role in the db).
                services.AddAuthorizationBuilder()
                    .AddPolicy(AuthConstants.GlobalAuthPolicy,
                        policy => policy.AddRequirements(new UserExistsRequirement()))
                    .SetFallbackPolicy(
                        new AuthorizationPolicyBuilder()
                            .AddRequirements(new UserExistsRequirement())
                            .Build());
                services.AddScoped<IAuthorizationHandler, UserExistsAuthorizationHandler>();
                services.AddScoped<IClaimsTransformation, ClaimsTransformer>();
                break;
            case "AzureAd":
                services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApp(configuration.GetSection("AzureAd"));
                services.AddControllersWithViews()
                    .AddMicrosoftIdentityUI(); // Handles Entra ID authentication
                // With Entra ID authentication, a user can be authenticated but not authorized.
                // For the global policy, we require that the user exists (i.e., has at least one role in the db).
                services.AddAuthorizationBuilder()
                    .AddPolicy(AuthConstants.GlobalAuthPolicy,
                        policy => policy.AddRequirements(new UserExistsRequirement()));
                // Do not set a fallback policy,
                // as this will prevent CSS and other static files from being served on the AccessDenied page.
                services.AddScoped<IAuthorizationHandler, UserExistsAuthorizationHandler>();
                services.AddScoped<IClaimsTransformation, ClaimsTransformer>();
                // Change the built-in MicrosoftIdentity/Account/AccessDenied endpoint to a custom AccessDenied page.
                // The built-in AccessDenied page is protected, which causes a redirect loop when trying to access it.
                services.Configure<CookieAuthenticationOptions>(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    options => options.AccessDeniedPath = "/AccessDenied");
                break;
            case "Ldap":
                services.AddScoped<IAuthHandler, LdapAuthHandler>();
                services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(options =>
                    {
                        options.LoginPath = "/login";
                        options.ReturnUrlParameter = "redirectUrl";
                    });
                // With Ldap authentication, a user is de facto authorized if they are authenticated.
                // Only require authentication for the global policy.
                services.AddAuthorizationBuilder()
                    .AddPolicy(AuthConstants.GlobalAuthPolicy, policy => policy.RequireAuthenticatedUser());
                // Do not set a fallback policy,
                // as that will prevent CSS and other static files from being served on the login page.
                
                // With Ldap authentication, role claims are being added in Login.razor
                // => no need to add claims transformation.
                break;
            default:
                throw new ArgumentException($"Invalid Authentication setting: {authentication}");
        }
        return services;
    }
}