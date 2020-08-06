using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EtlManager.Data;
using EtlManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EtlManager.Pages.Jobs
{
    public class SchedulesModel : PageModel
    {

        private readonly EtlManagerContext _context;

        public SchedulesModel(EtlManagerContext context)
        {
            _context = context;
        }

        public Job Job { get; set; }
        public IList<Schedule> Schedules { get; set; }

        [BindProperty]
        public Schedule NewSchedule { get; set; }

        public async Task OnGetAsync(Guid id)
        {
            Job = await _context.Jobs.Include(job => job.Schedules).FirstOrDefaultAsync(job => job.JobId == id);
            Schedules = Job.Schedules.OrderBy(s => s.TimeHours).ToList();

            NewSchedule = new Schedule { JobId = id };
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid? id, Guid jobId)
        {
            if (id == null) return NotFound();

            Schedule schedule = await _context.Schedules.FindAsync(id);

            if (schedule == null) return NotFound();

            _context.Schedules.Remove(schedule);
            await _context.SaveChangesAsync();
            
            Job = await _context.Jobs.Include(job => job.Schedules).FirstOrDefaultAsync(job => job.JobId == schedule.JobId);
            Schedules = Job.Schedules.OrderBy(s => s.TimeHours).ToList();
            return RedirectToPage("./Index", new { id = jobId });
        }

        public async Task<IActionResult> OnPost()
        {

            _context.Schedules.Add(NewSchedule);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index", new { id = NewSchedule.JobId });
        }
    }
}
