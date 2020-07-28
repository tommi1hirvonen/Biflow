using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ExecutorManager.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace ExecutorManager.Pages
{
    public class LogInModel : PageModel
    {

        private readonly IConfiguration _configuration;

        public LogInModel(IConfiguration configuration, ExecutorManager.Data.ExecutorManagerContext context)
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
                    if (Utility.AuthenticateUser(_configuration, Login.Username, Login.Password))
                    {
                        await SignInUser(Login.Username, false);
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

        private async Task SignInUser(string username, bool isPersistent)
        {
            var claims = new List<Claim>();
            try
            {
                claims.Add(new Claim(ClaimTypes.Name, username));
                var claimIdenties = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimPrincipal = new ClaimsPrincipal(claimIdenties);
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
