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
    public class DeleteModel : PageModel
    {
        private readonly ExecutorManager.Data.ExecutorManagerContext _context;

        public DeleteModel(ExecutorManager.Data.ExecutorManagerContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Step Step { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Step = await _context.Steps.FirstOrDefaultAsync(m => m.StepId == id);

            if (Step == null)
            {
                return NotFound();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Step = await _context.Steps.FindAsync(id);

            if (Step != null)
            {
                _context.Steps.Remove(Step);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index", new { id = Step.JobId });
        }
    }
}
