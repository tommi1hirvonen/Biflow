using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using EtlManager.Data;
using EtlManager.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EtlManager.Pages
{
    public class PasswordModel : PageModel
    {

        private readonly IConfiguration _configuration;
        private EtlManagerContext _context;
        private HttpContext _httpContext;

        public PasswordModel(IConfiguration configuration, EtlManagerContext context, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _context = context;
            _httpContext = httpContextAccessor.HttpContext;
        }

        public string Username { get; set; }

        [BindProperty]
        [Required]
        [MaxLength(250)]
        [MinLength(1)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string Password { get; set; }

        [BindProperty]
        [Required]
        [MaxLength(250)]
        [MinLength(1)]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        public string ConfirmPassword { get; set; }

        public bool Success = false;

        public void OnGet()
        {
            Username = _httpContext.User?.Identity?.Name;
        }

        public IActionResult OnPostChangePassword()
        {
            string username = _httpContext.User?.Identity?.Name;

            if (ModelState.ContainsKey("MatchError")) ModelState["MatchError"].Errors.Clear();
            if (ModelState.IsValid)
            {
                if (Password.Equals(ConfirmPassword))
                {
                    
                    if (Utility.UpdatePassword(_configuration, username, Password))
                    {
                        Success = true;
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Error updating password");
                    }
                }
                else
                {
                    ModelState.AddModelError("MatchError", "The two passwords do not match");
                }
            }

            return Page();
        }
        
    }
}
