using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EtlManager.Data;
using EtlManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EtlManager.Pages.Schedules
{
    public class IndexModel : PageModel
    {
        private readonly EtlManagerContext _context;

        public IndexModel(EtlManagerContext context)
        {
            _context = context;
        }

        public IList<Job> Jobs { get; set; }

        [BindProperty]
        public Schedule NewSchedule { get; set; } = new Schedule();

        public async Task OnGetAsync()
        {
            Jobs = await _context.Jobs.Include(job => job.Schedules).ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid? id)
        {
            if (id == null) return NotFound();

            Schedule schedule = await _context.Schedules.FindAsync(id);

            if (schedule == null) return NotFound();

            _context.Schedules.Remove(schedule);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _context.Schedules.Add(NewSchedule);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}