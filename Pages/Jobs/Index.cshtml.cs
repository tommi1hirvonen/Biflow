using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EtlManager.Data;
using EtlManager.Models;
using Microsoft.Extensions.Configuration;

namespace EtlManager.Pages.Jobs
{
    public class IndexModel : PageModel
    {
        private readonly EtlManagerContext _context;
        private readonly IConfiguration _configuration;

        public IndexModel(IConfiguration configuration, EtlManagerContext context)
        {
            _context = context;
            _configuration = configuration;
        }

        public IList<Job> Jobs { get;set; }

        public async Task OnGetAsync()
        {
            Jobs = await _context.Jobs.ToListAsync();
        }

        public async Task<IActionResult> OnPostCopy(Guid id)
        {

            await Utility.JobCopy(_configuration, id);

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostDelete(Guid id)
        {
            if (id == null) return NotFound();

            Job job= await _context.Jobs.FindAsync(id);

            if (job == null) return NotFound();

            _context.Jobs.Remove(job);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

    }
}
