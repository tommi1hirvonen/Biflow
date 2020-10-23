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
    public class EmailModel : PageModel
    {

        private readonly IConfiguration _configuration;
        private EtlManagerContext _context;
        private HttpContext _httpContext;

        public EmailModel(IConfiguration configuration, EtlManagerContext context, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _context = context;
            _httpContext = httpContextAccessor.HttpContext;
        }

        [BindProperty]
        public User User_ { get; set; }

        public bool Success = false;

        public void OnGet()
        {
            string username = _httpContext.User?.Identity?.Name;
            User_ = _context.Users.First(user => user.Username == username);
        }

        public IActionResult OnPostChangeEmail()
        {
            string username = _httpContext.User?.Identity?.Name;

            if (ModelState.IsValid && User_.Username == username) // Make sure the user is actually changing their own email address.
            {
                _context.Attach(User_).State = EntityState.Modified;
                _context.SaveChanges();
                Success = true;
            }
            
            return Page();
        }
        
    }
}
