using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ExecutorManager.Data;
using ExecutorManager.Models;


namespace ExecutorManager.Pages.Jobs
{
    public class IndexModel : PageModel
    {
        private readonly ExecutorManagerContext _context;

        public IndexModel(ExecutorManagerContext context)
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
