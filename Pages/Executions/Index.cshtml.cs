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
    public class IndexModel : PageModel
    {
        private readonly ExecutorManager.Data.ExecutorManagerContext _context;

        public IndexModel(ExecutorManager.Data.ExecutorManagerContext context)
        {
            _context = context;
        }

        public IList<Execution> Execution { get;set; }

        public async Task OnGetAsync()
        {
            List<Execution> execution = await _context.Executions.ToListAsync();
            Execution = execution.OrderByDescending(execution => execution.CreatedDateTime).ThenByDescending(execution => execution.StartDateTime).ToList();
        }
    }
}
