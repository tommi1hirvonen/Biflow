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

        public IList<Step> Step { get;set; }

        public Guid? JobId { get; set; }

        public async Task OnGetAsync(Guid? id)
        {
            JobId = id;
            List<Step> steps = await _context.Step.ToListAsync();
            Step = steps.Where(step => step.JobId == id).OrderBy(step => step.JobId).ThenBy(step => step.ExecutionPhase).ToList();
        }
    }
}
