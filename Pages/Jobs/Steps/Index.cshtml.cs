using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ExecutorManager.Data;
using ExecutorManager.Models;
using System.Diagnostics;

namespace ExecutorManager.Pages.Jobs.Steps
{
    public class IndexModel : PageModel
    {
        private readonly ExecutorManager.Data.ExecutorManagerContext _context;

        public IndexModel(ExecutorManager.Data.ExecutorManagerContext context)
        {
            _context = context;
        }

        public IList<Step> Steps { get;set; }

        public Job Job { get; set; }

        public async Task OnGetAsync(Guid id)
        {
            Job = await _context.Jobs.Include(job => job.Steps).FirstOrDefaultAsync(job => job.JobId == id);
            Steps = Job.Steps.OrderBy(step => step.ExecutionPhase).ToList();
        }
    }
}
