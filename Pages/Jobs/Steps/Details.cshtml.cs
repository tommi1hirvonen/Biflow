using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EtlManager.Data;
using EtlManager.Models;

namespace EtlManager.Pages.Jobs.Steps
{
    public class DetailsModel : PageModel
    {
        private readonly EtlManager.Data.EtlManagerContext _context;

        public DetailsModel(EtlManager.Data.EtlManagerContext context)
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

            Step = await _context.Steps.FirstOrDefaultAsync(m => m.StepId == id);

            if (Step == null)
            {
                return NotFound();
            }
            return Page();
        }
    }
}
