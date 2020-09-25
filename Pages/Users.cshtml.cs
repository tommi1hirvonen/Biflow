using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using EtlManager.Data;
using EtlManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EtlManager.Pages
{
    [Authorize(Policy = "RequireAdmin")]
    public class UsersModel : PageModel
    {
        private readonly EtlManagerContext context;
        private readonly IConfiguration configuration;

        public UsersModel(IConfiguration configuration, EtlManagerContext context)
        {
            this.context = context;
            this.configuration = configuration;
        }

        public IList<RoleUser> Users { get; set; }

        public RoleUser NewUser { get; set; }

        public RoleUser EditUser { get; set; }

        [BindProperty]
        [Required]
        [MaxLength(250)]
        [MinLength(1)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [BindProperty]
        [Required]
        [MaxLength(250)]
        [MinLength(1)]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }

        public async Task OnGet()
        {
            Users = await context.EditableUsers.ToListAsync();
        }

        public IActionResult OnPostCreateAsync([Bind("Username", "Email", "CreatedDateTime", "Role")] RoleUser NewUser)
        {
            if (Password.Equals(ConfirmPassword))
            {
                Utility.AddUser(configuration, NewUser, Password);
            }

            return RedirectToPage("./Users");
        }

        public async Task<IActionResult> OnPostEditAsync([Bind("Username", "Email", "CreatedDateTime", "Role")] RoleUser EditUser)
        {
            context.Attach(EditUser).State = EntityState.Modified;

            await context.SaveChangesAsync();

            return RedirectToPage("./Users");
        }

        public async Task<IActionResult> OnPostDeleteAsync(string username)
        {
            if (username == null) return NotFound();

            RoleUser user = await context.EditableUsers.FindAsync(username);

            if (user == null) return NotFound();

            context.EditableUsers.Remove(user);

            await context.SaveChangesAsync();

            return RedirectToPage("./Users");
        }
    }
}