using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EtlManager.Data;
using EtlManager.Models;

namespace EtlManager.Pages.Executions
{
    public class DetailsModel : PageModel
    {
        private readonly EtlManager.Data.EtlManagerContext _context;

        public DetailsModel(EtlManager.Data.EtlManagerContext context)
        {
            _context = context;
        }

        public Execution Execution { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Execution = await _context.Executions.FirstOrDefaultAsync(m => m.ExecutionId == id);

            if (Execution == null)
            {
                return NotFound();
            }
            return Page();
        }
    }
}
