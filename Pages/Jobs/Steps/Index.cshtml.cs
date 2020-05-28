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
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace ExecutorManager.Pages.Jobs.Steps
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly ExecutorManager.Data.ExecutorManagerContext _context;

        public IndexModel(IConfiguration configuration, ExecutorManager.Data.ExecutorManagerContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        public IList<Step> Steps { get;set; }

        public Job Job { get; set; }

        public async Task OnGetAsync(Guid id)
        {
            Job = await _context.Jobs.Include(job => job.Steps).FirstOrDefaultAsync(job => job.JobId == id);
            Steps = Job.Steps.OrderBy(step => step.ExecutionPhase).ToList();
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Job = await _context.Jobs.FindAsync(id);

            if (Job == null)
            {
                return NotFound();
            }

            await Utility.StartExecution(_configuration, Job);

            return RedirectToPage("../../Executions/Index");
        }
    }
}
