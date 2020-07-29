using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Configuration;

namespace ExecutorManager.Pages
{
    public class AccountModel : PageModel
    {

        private readonly IConfiguration _configuration;

        public AccountModel(IConfiguration configuration, ExecutorManager.Data.ExecutorManagerContext context)
        {
            _configuration = configuration;
        }

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

        public bool success = false;

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (ModelState.ContainsKey("MatchError")) ModelState["MatchError"].Errors.Clear();
            if (ModelState.IsValid)
            {
                if (Password.Equals(ConfirmPassword))
                {
                    var username = User.FindFirstValue(ClaimTypes.Name);
                    if (Utility.UpdatePassword(_configuration, username, Password))
                    {
                        success = true;
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
