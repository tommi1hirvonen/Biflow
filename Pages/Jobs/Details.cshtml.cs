using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EtlManager.Data;
using EtlManager.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace EtlManager.Pages.Jobs
{
    public class DetailsModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly EtlManager.Data.EtlManagerContext _context;

        public DetailsModel(IConfiguration configuration, EtlManager.Data.EtlManagerContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        public Job Job { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Job = await _context.Jobs.FirstOrDefaultAsync(m => m.JobId == id);

            if (Job == null)
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

            Job = await _context.Jobs.FindAsync(id);

            if (Job == null)
            {
                return NotFound();
            }

            await Utility.StartExecution(_configuration, Job);

            return RedirectToPage("../Executions/Index");
        }
    }
}
