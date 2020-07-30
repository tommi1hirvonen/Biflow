using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication.ExtendedProtection;
using System.Threading.Tasks;
using ExecutorManager.Data;
using ExecutorManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExecutorManager.Pages.Jobs.Schedules
{
    public class CreateModel : PageModel
    {
        private readonly ExecutorManagerContext _context;

        public CreateModel(ExecutorManagerContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Schedule Schedule { get; set; }

        [BindProperty]
        public string OnMinutes { get; set; } = "0";

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

            switch (OnMinutes)
            {
                case "15":
                    Schedule.On15Minutes = true;
                    break;
                case "30":
                    Schedule.On30Minutes = true;
                    break;
                case "45":
                    Schedule.On45Minutes = true;
                    break;
                default:
                    Schedule.On00Minutes = true;
                    break;
            }

            _context.Schedules.Add(Schedule);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index", new { id = Schedule.JobId });
        }
    }
}
