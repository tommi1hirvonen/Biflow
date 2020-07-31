using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EtlManager.Data;
using EtlManager.Models;

namespace EtlManager.Pages.Jobs.Steps
{
    public class EditModel : PageModel
    {
        private readonly EtlManager.Data.EtlManagerContext _context;

        public EditModel(EtlManager.Data.EtlManagerContext context)
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

            Step = await _context.Steps.Include(step => step.Job).FirstOrDefaultAsync(m => m.StepId == id);

            if (Step == null)
            {
                return NotFound();
            }
            return Page();
        }


        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // The JobId property of the Job member of Step is lost. Restore it before saving.
            // Otherwise Entity Framework will try to generate a new Job and place the step under it.
            Step.Job.JobId = Step.JobId;
            _context.Attach(Step).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StepExists(Step.StepId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index", new { id = Step.JobId});
        }

        private bool StepExists(Guid id)
        {
            return _context.Steps.Any(e => e.StepId == id);
        }
    }
}
