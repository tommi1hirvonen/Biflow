using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using EtlManager.Data;
using EtlManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EtlManager.Pages.Jobs.JobDetails
{
    public class HistoryModel : PageModel
    {
        private readonly EtlManagerContext _context;
        public HistoryModel(EtlManagerContext context)
        {
            _context = context;
        }

        public Job Job { get; set; }

        public IList<Job> Jobs { get; set; }

        public IList<JobExecution> Executions { get; set; }

        public int MaxExecutions { get; set; } = 50;

        [DisplayFormat(DataFormatString = "{0:N0}%")]
        public decimal AverageSuccessRate { get; set; }
        public int AverageDurationInSeconds { get; set; }

        public async Task OnGetAsync(Guid id)
        {
            Jobs = await _context.Jobs.OrderBy(job => job.JobName).ToListAsync();
            Job = Jobs.First(job => job.JobId == id);

            Executions = await _context.JobExecutions
                .Where(execution => execution.JobId == Job.JobId)
                .OrderByDescending(execution => execution.CreatedDateTime)
                .ThenByDescending(Execution => Execution.StartDateTime)
                .Take(MaxExecutions)
                .ToListAsync();

            if (Executions.Count > 0)
            {
                AverageSuccessRate = (decimal)Executions.Where(e => e.ExecutionStatus == "COMPLETED").Count() / Executions.Count() * 100;
            }
            else
            {
                AverageSuccessRate = 0;
            }
            
            AverageDurationInSeconds = (int)(Executions.Average(e => e.ExecutionInSeconds) ?? 0);
        }
    }
}