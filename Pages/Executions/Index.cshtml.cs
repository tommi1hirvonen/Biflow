using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ExecutorManager.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ExecutorManager.Pages.Executions
{
    public class IndexModel : PageModel
    {
        private readonly ExecutorManager.Data.ExecutorManagerContext _context;

        public IndexModel(ExecutorManager.Data.ExecutorManagerContext context)
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
        public DateTime DateTimeUntil { get; set; } = DateTime.Now.Trim(TimeSpan.TicksPerMinute);

        [BindProperty(SupportsGet = true)]
        public int IntervalHours { get; set; } = 3;


        public IList<Execution> Execution { get;set; }

        public async Task OnGetAsync()
        {
            IQueryable<Execution> executions = _context.Executions;

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

            executions = executions
                .Where(execution => execution.CreatedDateTime <= DateTimeUntil)
                .Where(execution => execution.CreatedDateTime >= DateTimeUntil.AddHours(-IntervalHours));

            Execution = await executions.OrderByDescending(execution => execution.CreatedDateTime).ThenByDescending(execution => execution.StartDateTime).ToListAsync();
            
        }

    }
}
