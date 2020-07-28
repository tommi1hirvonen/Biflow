using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ExecutorManager.Data;
using ExecutorManager.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ExecutorManager.Pages.Jobs
{
    public class IndexModel : PageModel
    {
        private readonly ExecutorManager.Data.ExecutorManagerContext _context;

        public IndexModel(ExecutorManager.Data.ExecutorManagerContext context)
        {
            _context = context;
        }

        public IList<Job> Job { get;set; }

        public async Task OnGetAsync()
        {
            Job = await _context.Jobs.ToListAsync();
        }

    }
}
