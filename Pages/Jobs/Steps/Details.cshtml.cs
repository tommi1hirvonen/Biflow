using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ExecutorManager.Data;
using ExecutorManager.Models;

namespace ExecutorManager.Pages.Jobs.Steps
{
    public class DetailsModel : PageModel
    {
        private readonly ExecutorManager.Data.ExecutorManagerContext _context;

        public DetailsModel(ExecutorManager.Data.ExecutorManagerContext context)
        {
            _context = context;
        }

        public Step Step { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Step = await _context.Step.FirstOrDefaultAsync(m => m.StepId == id);

            if (Step == null)
            {
                return NotFound();
            }
            return Page();
        }
    }
}
