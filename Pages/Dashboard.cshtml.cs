using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Schema;
using EtlManager.Data;
using EtlManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace EtlManager.Pages
{
    public class DashboardModel : PageModel
    {
        private readonly EtlManagerContext _context;

        public DashboardModel(EtlManagerContext context)
        {
            _context = context;
        }

        public IList<JobExecution> JobExecutions { get; set; }

        public Dictionary<string, Dictionary<DateTime, int>> Executions { get; set; } = new Dictionary<string, Dictionary<DateTime, int>>();

        public void OnGet()
        {
            var executions = _context.JobExecutions
                .Where(e => e.ExecutionInSeconds > 0)
                .OrderBy(e => e.CreatedDateTime)
                .GroupBy(group => new {
                    group.JobName,
                    ((DateTime)group.CreatedDateTime).Date
                })
                .Select(select => new {
                    Duration = select.Average(total => total.ExecutionInSeconds),
                    select.Key.JobName,
                    select.Key.Date
                })
                .ToList();

            var jobNames = executions.Select(s => s.JobName).Distinct().ToList();
            foreach (var name in jobNames)
            {
                Dictionary<DateTime, int> d = new Dictionary<DateTime, int>();
                var e = executions.Where(e => e.JobName == name).ToList();
                e.ForEach(e => d[e.Date] = (int)e.Duration);
                Executions[name] = d;
            }
        }
    }
}