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

namespace EtlManager.Pages.Jobs.JobDetails.StepDetails
{
    public class StepHistoryModel : PageModel
    {
        private readonly EtlManagerContext _context;

        public StepHistoryModel(EtlManagerContext context)
        {
            _context = context;
        }

        public Step Step { get; set; }

        public Job Job { get; set; }

        public IList<StepExecution> Executions { get; set; }

        public int MaxExecutions { get; set; } = 50;

        [DisplayFormat(DataFormatString = "{0:N0}%")]
        public decimal AverageSuccessRate { get; set; }
        public int AverageDurationInSeconds { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {

            if (id == null)
            {
                return NotFound();
            }

            Step = await _context.Steps
                .Include(step => step.Parameters)
                .Include(step => step.Dependencies)
                .ThenInclude(dependency => dependency.DependantOnStep)
                .FirstOrDefaultAsync(m => m.StepId == id);

            Job = await _context.Jobs.Include(job => job.Steps).AsNoTracking().FirstOrDefaultAsync(job => job.JobId == Step.JobId);

            Executions = await _context.Executions
                .Where(execution => execution.StepId == Step.StepId)
                .OrderByDescending(execution => execution.CreatedDateTime)
                .ThenByDescending(Execution => Execution.StartDateTime)
                .Take(MaxExecutions)
                .ToListAsync();

            AverageSuccessRate = (decimal)Executions.Where(e => e.ExecutionStatus == "COMPLETED").Count() / Executions.Count() * 100;
            AverageDurationInSeconds = (int)(Executions.Average(e => e.ExecutionInSeconds) ?? 0);

            return Page();
        }
    }
}