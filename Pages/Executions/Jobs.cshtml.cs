using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using EtlManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EtlManager.Pages.Executions
{
    public class JobsModel : PageModel
    {
        private readonly Data.EtlManagerContext _context;

        public JobsModel(Data.EtlManagerContext context)
        {
            _context = context;
        }

        public SelectList Statuses { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Status { get; set; }


        public SelectList JobNames { get; set; }

        [BindProperty(SupportsGet = true)]
        public string JobName { get; set; }


        [BindProperty(SupportsGet = true)]
        [DataType(DataType.DateTime)]
        public DateTime DateTimeUntil { get; set; } = DateTime.Now.Trim(TimeSpan.TicksPerMinute).AddHours(1);

        [BindProperty(SupportsGet = true)]
        public int IntervalHours { get; set; } = 3;


        public IList<JobExecution> Executions { get; set; }

        public async Task OnGetAsync()
        {
            IQueryable<JobExecution> executions = _context.JobExecutions;

            executions = executions
                .Where(execution => execution.CreatedDateTime <= DateTimeUntil)
                .Where(execution => execution.CreatedDateTime >= DateTimeUntil.AddHours(-IntervalHours));

            Statuses = new SelectList(await executions.Select(execution => execution.ExecutionStatus).Distinct().ToListAsync());
            JobNames = new SelectList(await executions.Select(execution => execution.JobName).Distinct().ToListAsync());

            if (!string.IsNullOrEmpty(Status))
            {
                executions = executions.Where(execution => execution.ExecutionStatus == Status);
            }

            if (!string.IsNullOrEmpty(JobName))
            {
                executions = executions.Where(execution => execution.JobName == JobName);
            }

            Executions = await executions.OrderByDescending(execution => execution.CreatedDateTime).ThenByDescending(execution => execution.StartDateTime).ToListAsync();

        }
    }
}
