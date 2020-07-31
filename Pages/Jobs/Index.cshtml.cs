using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EtlManager.Data;
using EtlManager.Models;


namespace EtlManager.Pages.Jobs
{
    public class IndexModel : PageModel
    {
        private readonly EtlManagerContext _context;

        public IndexModel(EtlManagerContext context)
        {
            _context = context;
        }

        public IList<Job> Jobs { get;set; }

        public async Task OnGetAsync()
        {
            Jobs = await _context.Jobs.ToListAsync();
        }

    }
}
