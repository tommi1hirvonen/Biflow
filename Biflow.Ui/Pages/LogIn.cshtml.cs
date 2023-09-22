using Biflow.DataAccess.Models;
using Biflow.Ui.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Authentication;
using System.Security.Claims;

namespace Biflow.Ui.Pages;

public class LogInModel : PageModel
{
    private readonly IAuthHandler _authHandler;

    public LogInModel(IAuthHandler authHandler)
    {
        _authHandler = authHandler;
    }

    [BindProperty]
    public Login Login { get; set; } = new();

    public IActionResult OnGet()
    {
        if (HttpContext?.User?.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(Url.Content("~/"));
        }
        else
        {
            return Page();
        }
    }

    public async Task<IActionResult> OnPostLogIn()
    {
        if (!ModelState.IsValid || Login.Username is null || Login.Password is null)
        {
            return Page();
        }

        try
        {
            var roles = await _authHandler.AuthenticateUserAsync(Login.Username, Login.Password);
            await SignInUser(Login.Username, roles, false);
            return LocalRedirect(Url.Content("~/"));
        }
        catch (InvalidCredentialException)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password");
        }
        catch (AuthenticationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Login error");
        }

        return Page();
    }

    private async Task SignInUser(string username, IEnumerable<string> roles, bool isPersistent)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username)
        };
        var roleClaims = roles.Select(role => new Claim(ClaimTypes.Role, role));
        claims.AddRange(roleClaims);
        var claimIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var claimPrincipal = new ClaimsPrincipal(claimIdentity);
        var authenticationProperties = new AuthenticationProperties()
        {
            IsPersistent = isPersistent,
            ExpiresUtc = DateTime.UtcNow.AddMinutes(30)
        };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimPrincipal, authenticationProperties);
    }
}
