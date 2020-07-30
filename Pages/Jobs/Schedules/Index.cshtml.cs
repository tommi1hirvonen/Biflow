using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExecutorManager.Data;
using ExecutorManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ExecutorManager.Pages.Jobs
{
    public class SchedulesModel : PageModel
    {

        private readonly ExecutorManagerContext _context;

        public SchedulesModel(ExecutorManagerContext context)
        {
            _context = context;
        }

        public Job Job { get; set; }
        public IList<Schedule> Schedules { get; set; }

        public async Task OnGetAsync(Guid id)
        {
            Job = await _context.Jobs.Include(job => job.Schedules).FirstOrDefaultAsync(job => job.JobId == id);
            Schedules = Job.Schedules.OrderBy(s => s.TimeHours).ToList();
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid? id)
        {
            if (id == null) return NotFound();

            Schedule schedule = await _context.Schedules.FindAsync(id);

            if (schedule == null) return NotFound();

            _context.Schedules.Remove(schedule);
            await _context.SaveChangesAsync();
            
            Job = await _context.Jobs.Include(job => job.Schedules).FirstOrDefaultAsync(job => job.JobId == schedule.JobId);
            Schedules = Job.Schedules.OrderBy(s => s.TimeHours).ToList();
            return Page();
        }
    }
}
