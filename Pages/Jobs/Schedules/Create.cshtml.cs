using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication.ExtendedProtection;
using System.Threading.Tasks;
using EtlManager.Data;
using EtlManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EtlManager.Pages.Jobs.Schedules
{
    public class CreateModel : PageModel
    {
        private readonly EtlManagerContext _context;

        public CreateModel(EtlManagerContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Schedule Schedule { get; set; }

        public IActionResult OnGet(Guid id)
        {
            Schedule = new Schedule { JobId = id };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (!Schedule.Monday && !Schedule.Tuesday && !Schedule.Wednesday && !Schedule.Thursday
                && !Schedule.Friday && !Schedule.Saturday && !Schedule.Sunday)
            {
                ModelState.AddModelError(string.Empty, "Select at least one weekday");
                return Page();
            }

            if (Schedule.TimeMinutes != 0 && Schedule.TimeMinutes != 15 && Schedule.TimeMinutes != 30 && Schedule.TimeMinutes != 45)
            {
                ModelState.AddModelError(string.Empty, "Incorrect minutes value");
                return Page();
            }

            _context.Schedules.Add(Schedule);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index", new { id = Schedule.JobId });
        }
    }
}
