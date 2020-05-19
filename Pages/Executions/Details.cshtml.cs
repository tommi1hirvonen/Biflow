using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ExecutorManager.Data;
using ExecutorManager.Models;

namespace ExecutorManager.Pages.Executions
{
    public class DetailsModel : PageModel
    {
        private readonly ExecutorManager.Data.ExecutorManagerContext _context;

        public DetailsModel(ExecutorManager.Data.ExecutorManagerContext context)
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
