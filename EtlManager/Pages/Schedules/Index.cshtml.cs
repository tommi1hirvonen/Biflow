using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EtlManager.Data;
using EtlManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EtlManager.Pages.Schedules
{
    public class IndexModel : PageModel
    {
        private readonly EtlManagerContext _context;
        private readonly IConfiguration _configuration;
        private readonly IAuthorizationService _authorizationService;

        public IndexModel(IConfiguration configuration, EtlManagerContext context, IAuthorizationService authorizationService)
        {
            _context = context;
            _configuration = configuration;
            _authorizationService = authorizationService;
        }

        public IList<Job> Jobs { get; set; }

        [BindProperty]
        public Schedule NewSchedule { get; set; } = new Schedule { IsEnabled = true };

        [BindProperty]
        public ScheduleGeneration ScheduleGeneration { get; set; }

        public bool IsOperator { get; set; } = false;

        public async Task OnGetAsync()
        {
            Jobs = await _context.Jobs.Include(job => job.Schedules).OrderBy(job => job.JobName).ToListAsync();

            var authorized = await _authorizationService.AuthorizeAsync(User, "RequireOperator");
            if (authorized.Succeeded)
            {
                IsOperator = true;
            }
        }

        public async Task<IActionResult> OnPostDelete(Guid? id)
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

        public async Task<IActionResult> OnPostCreate()
        {
            var authorized = await _authorizationService.AuthorizeAsync(User, "RequireOperator");
            if (!authorized.Succeeded)
            {
                return Forbid();
            }

            _context.Schedules.Add(NewSchedule);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
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

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostToggleEnabled(Guid? id)
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

            await Utility.ToggleScheduleEnabled(_configuration, schedule);

            return new JsonResult(new { success = true });
        }
    }

    public class ScheduleGenerationOptions
    {
        public Interval IntervalType { get; set; }
        public int IntevalValue { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    public enum Interval
    {
        Minute,
        Hour
    }
}