using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EtlManager.Data;
using EtlManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EtlManager.Pages.Jobs.Steps
{
    public class DependencyModel : PageModel
    {
        private readonly EtlManagerContext _context;

        public DependencyModel(EtlManagerContext context)
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
