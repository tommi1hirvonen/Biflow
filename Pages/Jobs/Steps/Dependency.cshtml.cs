using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExecutorManager.Data;
using ExecutorManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ExecutorManager.Pages.Jobs.Steps
{
    public class DependencyModel : PageModel
    {
        private readonly ExecutorManagerContext _context;

        public DependencyModel(ExecutorManagerContext context)
        {
            _context = context;
        }

        public Guid JobId { get; set; }
        public string JobName { get; set; }

        public IList<Dependency> Dependencies { get; set; }

        public async Task OnGetAsync(Guid id, string name)
        {
            JobId = id;
            JobName = name;

            Dependencies = await _context.Dependencies
                .Where(d => d.JobId == id)
                .OrderBy(d => d.StepName)
                .ThenBy(d => d.DependantOnStepName).ToListAsync();
        }
    }
}
