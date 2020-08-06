using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using EtlManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EtlManager.Pages.Executions
{
    public class JobDetailsModel : PageModel
    {
        private readonly Data.EtlManagerContext _context;

        public JobDetailsModel(Data.EtlManagerContext context)
        {
            _context = context;
        }

        public Guid ExecutionId { get; set; }

        public IList<StepExecution> Executions { get; set; }

        public JobExecution JobExecution { get; set; }

        public async Task OnGetAsync(Guid id)
        {
            ExecutionId = id;
            Executions = await _context.Executions
                .Where(e => e.ExecutionId == id)
                .OrderBy(execution => execution.CreatedDateTime)
                .ThenBy(execution => execution.StartDateTime)
                .ToListAsync();
            JobExecution = await _context.JobExecutions
                .FirstOrDefaultAsync(e => e.ExecutionId == id);
        }
    }
}