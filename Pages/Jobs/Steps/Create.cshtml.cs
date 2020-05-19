using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ExecutorManager.Data;
using ExecutorManager.Models;

namespace ExecutorManager.Pages.Jobs.Steps
{
    public class CreateModel : PageModel
    {
        private readonly ExecutorManager.Data.ExecutorManagerContext _context;

        public CreateModel(ExecutorManager.Data.ExecutorManagerContext context)
        {
            _context = context;
        }

        public Guid JobId { get; set; }

        public IActionResult OnGet(Guid id)
        {
            JobId = id;
            Step = new Step
            {
                JobId = JobId
            };
            return Page();
        }

        [BindProperty]
        public Step Step { get; set; }

        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {

            if (!ModelState.IsValid)
            {
                return Page();
            }
            
            _context.Steps.Add(Step);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index", new { id = Step.JobId });
        }
    }
}
