using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EtlManager.Data;
using EtlManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EtlManager.Pages.Jobs.JobDetails
{
    public class DependenciesModel : PageModel
    {
        private readonly EtlManagerContext _context;

        public DependenciesModel(EtlManagerContext context)
        {
            _context = context;
        }

        public Job Job { get; set; }

        public IList<Dependency> Dependencies { get; set; }

        public async Task OnGetAsync(Guid id)
        {
            Job = await _context.Jobs.FindAsync(id);

            Dependencies = await _context.Dependencies.Include(d => d.Step).Include(d => d.DependantOnStep)
                .Where(d => d.Step.JobId == id)
                .OrderBy(d => d.Step.StepName)
                .ThenBy(d => d.DependantOnStep.StepName).ToListAsync();
        }
    }
}
