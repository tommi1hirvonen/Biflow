using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EtlManager.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace EtlManager.Pages.Executions
{
    public class StepsModel : PageModel
    {
        private readonly Data.EtlManagerContext _context;

        public StepsModel(Data.EtlManagerContext context)
        {
            _context = context;
        }

        public SelectList Statuses { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string Status { get; set; }


        public SelectList StepNames { get; set; }

        [BindProperty(SupportsGet = true)]
        public string StepName { get; set; }


        public SelectList JobNames { get; set; }

        [BindProperty(SupportsGet = true)]
        public string JobName { get; set; }


        [BindProperty(SupportsGet = true)]
        [DataType(DataType.DateTime)]
        public DateTime DateTimeUntil { get; set; } = DateTime.Now.Trim(TimeSpan.TicksPerMinute).AddMinutes(1);

        [BindProperty(SupportsGet = true)]
        public int IntervalHours { get; set; } = 3;


        public IList<StepExecution> Executions { get;set; }

        public async Task OnGetAsync(long? dateTimeUntilTicks)
        {
            IQueryable<StepExecution> executions = _context.Executions;

            // If dateTimeUntilTicks was provided, use that.
            DateTimeUntil = dateTimeUntilTicks?.ToDateTime() ?? DateTimeUntil;

            executions = executions
                .Where(execution => execution.CreatedDateTime <= DateTimeUntil)
                .Where(execution => execution.CreatedDateTime >= DateTimeUntil.AddHours(-IntervalHours));

            Statuses = new SelectList(await executions.Select(execution => execution.ExecutionStatus).Distinct().ToListAsync());
            StepNames = new SelectList(await executions.Select(execution => execution.StepName).Distinct().ToListAsync());
            JobNames = new SelectList(await executions.Select(execution => execution.JobName).Distinct().ToListAsync());

            if (!string.IsNullOrEmpty(Status))
            {
                executions = executions.Where(execution => execution.ExecutionStatus == Status);
            }

            if (!string.IsNullOrEmpty(StepName))
            {
                executions = executions.Where(execution => execution.StepName == StepName);
            }

            if (!string.IsNullOrEmpty(JobName))
            {
                executions = executions.Where(execution => execution.JobName == JobName);
            }

            Executions = await executions.OrderByDescending(execution => execution.CreatedDateTime).ThenByDescending(execution => execution.StartDateTime).ToListAsync();
            
        }

    }
}
