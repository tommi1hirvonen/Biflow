using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using EtlManagerUi.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace EtlManagerUi.Pages
{
    public class LogInModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public LogInModel(IConfiguration configuration, EtlManagerUi.Data.EtlManagerContext context)
        {
            _configuration = configuration;
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
            try
            {
                if (ModelState.IsValid && Login.Username is not null && Login.Password is not null)
                {
                    AuthenticationResult result = Utility.AuthenticateUser(_configuration, Login.Username, Login.Password);
                    if (result.AuthenticationSuccessful && result.Role is not null)
                    {
                        await SignInUser(Login.Username, result.Role, false);
                        return LocalRedirect(Url.Content("~/"));
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Invalid username or password.");
                    }
                }
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "Login error");
            }

            return Page();
        }

        private async Task SignInUser(string username, string role, bool isPersistent)
        {
            var claims = new List<Claim>();
            try
            {
                claims.Add(new Claim(ClaimTypes.Name, username));
                claims.Add(new Claim(ClaimTypes.Role, role));
                var claimIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimPrincipal = new ClaimsPrincipal(claimIdentity);
                var authenticationProperties = new AuthenticationProperties() { IsPersistent = isPersistent, ExpiresUtc = DateTime.UtcNow.AddMinutes(30) };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimPrincipal, authenticationProperties);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
