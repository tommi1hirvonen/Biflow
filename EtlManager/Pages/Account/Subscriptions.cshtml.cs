using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EtlManager.Data;
using EtlManager.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EtlManager.Pages.Account
{
    public class SubscriptionsModel : PageModel
    {
        private readonly EtlManagerContext _context;
        private readonly HttpContext _httpContext;

        public SubscriptionsModel(EtlManagerContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContext = httpContextAccessor.HttpContext;
        }

        public User User_ { get; set; }

        public IList<Job> Jobs { get; set; }

        public async Task OnGetAsync()
        {
            string username = _httpContext.User?.Identity?.Name;
            User_ = await _context.Users
                .Include(user => user.Subscriptions)
                .FirstOrDefaultAsync(user => user.Username == username);

            Jobs = await _context.Jobs.OrderBy(job => job.JobName).ToListAsync();
        }

        public async Task<IActionResult> OnPostToggleSubscribed(Guid id)
        {
            string username = _httpContext.User?.Identity?.Name;

            var subscription = await _context.Subscriptions
                .Where(subscription => subscription.JobId == id && subscription.Username == username)
                .FirstOrDefaultAsync();

            try
            {
                if (subscription != null)
                {
                    _context.Subscriptions.Remove(subscription);
                }
                else
                {
                    _context.Subscriptions.Add(new Subscription { JobId = id, Username = username });
                }
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, responseText = "Error toggling subscription: " + ex.Message });
            }

            return new JsonResult(new { success = true });
        }
    }
}