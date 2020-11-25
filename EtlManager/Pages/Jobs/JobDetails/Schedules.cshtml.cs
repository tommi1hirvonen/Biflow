using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EtlManager.Data;
using EtlManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EtlManager.Pages.Jobs.JobDetails
{
    public class SchedulesModel : PageModel
    {

        private readonly EtlManagerContext _context;
        public readonly string WebRootPath;
        private readonly HttpContext httpContext;
        private readonly IAuthorizationService _authorizationService;

        public SchedulesModel(EtlManagerContext context, IWebHostEnvironment webHostEnvironment, IHttpContextAccessor httpContextAccessor, IAuthorizationService authorizationService)
        {
            _context = context;
            WebRootPath = webHostEnvironment.WebRootPath;
            httpContext = httpContextAccessor.HttpContext;
            _authorizationService = authorizationService;
        }

        public Job Job { get; set; }

        public IList<Job> Jobs { get; set; }

        public IList<Schedule> Schedules { get; set; }

        public bool Subscribed { get; set; }

        [BindProperty]
        public Schedule NewSchedule { get; set; }

        public bool IsOperator { get; set; } = false;
        public bool IsEditor { get; set; } = false;

        [BindProperty]
        public ScheduleGeneration ScheduleGeneration { get; set; }

        [BindProperty]
        public string NewJobName { get; set; }

        public async Task OnGetAsync(Guid id)
        {
            Jobs = await _context.Jobs.OrderBy(job => job.JobName).ToListAsync();
            Job = await _context.Jobs.Include(job => job.Schedules).Include(job => job.Subscriptions).FirstOrDefaultAsync(job => job.JobId == id);
            Schedules = Job.Schedules.OrderBy(s => s.TimeHours).ToList();

            NewSchedule = new Schedule { JobId = id, IsEnabled = true };

            string user = httpContext.User?.Identity?.Name;
            if (Job.Subscriptions.Select(subscription => subscription.Username).Contains(user))
            {
                Subscribed = true;
            }

            var authorized = await _authorizationService.AuthorizeAsync(User, "RequireOperator");
            if (authorized.Succeeded)
            {
                IsOperator = true;
            }

            var authorized2 = await _authorizationService.AuthorizeAsync(User, "RequireEditor");
            if (authorized2.Succeeded)
            {
                IsEditor = true;
            }
        }

        public async Task<IActionResult> OnPostDelete(Guid id)
        {
            var authorized = await _authorizationService.AuthorizeAsync(User, "RequireOperator");
            if (!authorized.Succeeded)
            {
                return new JsonResult(new { success = false, responseText = "Unauthorized" });
            }

            if (id == null)
            {
                return new JsonResult(new { success = false, responseText = "Id argument was null" });
            }

            Schedule schedule = await _context.Schedules.FindAsync(id);

            if (schedule == null)
            {
                return new JsonResult(new { success = false, responseText = "No schedule found for id " + id });
            }

            try
            {
                _context.Schedules.Remove(schedule);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, responseText = "Error deleting schedule: " + ex.Message });
            }

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPost()
        {
            var authorized = await _authorizationService.AuthorizeAsync(User, "RequireOperator");
            if (!authorized.Succeeded)
            {
                return Forbid();
            }

            _context.Schedules.Add(NewSchedule);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Schedules", new { id = NewSchedule.JobId });
        }

        public async Task<IActionResult> OnPostGenerate()
        {
            var authorized = await _authorizationService.AuthorizeAsync(User, "RequireOperator");
            if (!authorized.Succeeded)
            {
                return Forbid();
            }

            var startHours = int.Parse(ScheduleGeneration.StartTime.Split(':')[0]);
            var startMinutes = int.Parse(ScheduleGeneration.StartTime.Split(':')[1]);
            var endHours = int.Parse(ScheduleGeneration.EndTime.Split(':')[0]);
            var endMinutes = int.Parse(ScheduleGeneration.EndTime.Split(':')[1]);

            if (ScheduleGeneration.IntervalType == "Hours")
            {
                for (int hour = startHours; hour <= endHours; hour += ScheduleGeneration.IntervalValueHours)
                {
                    var schedule = new Schedule()
                    {
                        IsEnabled = true,
                        JobId = ScheduleGeneration.JobId,
                        TimeHours = hour,
                        TimeMinutes = startMinutes,
                        Monday = ScheduleGeneration.Monday,
                        Tuesday = ScheduleGeneration.Tuesday,
                        Wednesday = ScheduleGeneration.Wednesday,
                        Thursday = ScheduleGeneration.Thursday,
                        Friday = ScheduleGeneration.Friday,
                        Saturday = ScheduleGeneration.Saturday,
                        Sunday = ScheduleGeneration.Sunday
                    };
                    _context.Schedules.Add(schedule);
                }
            }
            else if (ScheduleGeneration.IntervalType == "Minutes")
            {
                var timeSpan = new TimeSpan(startHours, startMinutes, 0);
                var endTimeSpan = new TimeSpan(endHours, endMinutes, 0);
                while (timeSpan <= endTimeSpan)
                {
                    var schedule = new Schedule()
                    {
                        IsEnabled = true,
                        JobId = ScheduleGeneration.JobId,
                        TimeHours = timeSpan.Hours,
                        TimeMinutes = timeSpan.Minutes,
                        Monday = ScheduleGeneration.Monday,
                        Tuesday = ScheduleGeneration.Tuesday,
                        Wednesday = ScheduleGeneration.Wednesday,
                        Thursday = ScheduleGeneration.Thursday,
                        Friday = ScheduleGeneration.Friday,
                        Saturday = ScheduleGeneration.Saturday,
                        Sunday = ScheduleGeneration.Sunday
                    };
                    _context.Schedules.Add(schedule);
                    timeSpan = timeSpan.Add(new TimeSpan(0, ScheduleGeneration.IntervalValueMinutes, 0));
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("./Schedules", new { id = ScheduleGeneration.JobId });
        }

        public async Task<IActionResult> OnPostRenameJob(Guid id)
        {
            var authorized = await _authorizationService.AuthorizeAsync(User, "RequireEditor");
            if (!authorized.Succeeded)
            {
                return Forbid();
            }

            var job = _context.Jobs.Find(id);

            if (job == null)
            {
                return NotFound();
            }

            job.JobName = NewJobName;

            _context.Attach(job).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return RedirectToPage("./Schedules", new { id });
        }
    }
}
