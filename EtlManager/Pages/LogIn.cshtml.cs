using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using EtlManager.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace EtlManager.Pages
{
    public class LogInModel : PageModel
    {

        private readonly IConfiguration _configuration;

        public LogInModel(IConfiguration configuration, EtlManager.Data.EtlManagerContext context)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public Login Login { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostLogIn()
        {
            try
            {
                if (ModelState.IsValid)
                {
                    AuthenticationResult result = Utility.AuthenticateUser(_configuration, Login.Username, Login.Password);
                    if (result.AuthenticationSuccessful)
                    {
                        await SignInUser(Login.Username, result.Role, false);
                        return RedirectToPage("/Index");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Invalid username or password.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex);
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
                var authenticationManager = Request.HttpContext;

                await authenticationManager.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimPrincipal, new AuthenticationProperties() { IsPersistent = isPersistent });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
