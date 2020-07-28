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
using Microsoft.Extensions.Logging;

namespace ExecutorManager.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly ExecutorManager.Data.ExecutorManagerContext _context;

        public IndexModel(IConfiguration configuration, ExecutorManager.Data.ExecutorManagerContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        [BindProperty]
        public Login Login { get; set; }

        public IActionResult OnGet()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return RedirectToPage("/LogIn");
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostLogOff()
        {
            try
            {
                var authenticationManager = Request.HttpContext;
                await authenticationManager.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return RedirectToPage("/LogIn");
        }

    }
}
