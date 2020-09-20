using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Schema;
using EtlManager.Data;
using EtlManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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

        public Dictionary<string, List<TimeSeriesItem>> TimeSeriesItems { get; set; } = new Dictionary<string, List<TimeSeriesItem>>();

        public List<ReportingJob> Jobs { get; set; }

        public List<TopStep> TopFailedSteps { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool LoadReport { get; set; } = false;

        [BindProperty(SupportsGet = true)]
        public int IntervalDays { get; set; } = 90;

        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        public DateTime DateTimeUntil { get; set; } = DateTime.Now.Date.Trim(TimeSpan.TicksPerMinute).AddHours(1);

        public async Task OnGetAsync()
        {
            if (!LoadReport) return;

            var dateTimeStart = DateTimeUntil.AddDays(-IntervalDays);

            // Get duration executions
            var executions = await _context.JobExecutions
                .Where(e => e.ExecutionInSeconds > 0 && e.CreatedDateTime >= dateTimeStart && e.CreatedDateTime <= DateTimeUntil)
                .OrderBy(e => e.CreatedDateTime)
                .GroupBy(group => new
                {
                    group.JobName,
                    ((DateTime)group.CreatedDateTime).Date
                })
                .Select(select => new TimeSeriesItem
                {
                    DurationInMinutes = select.Average(total => (decimal)total.ExecutionInSeconds / 60),
                    JobName = select.Key.JobName,
                    Date = select.Key.Date,
                    NumberOfExecutions = select.Count()
                }).ToListAsync();

            TimeSeriesItems = executions
                .GroupBy(e => e.JobName)
                .ToDictionary(e => e.Key, e => e.ToList());


            // Job success rates
            Jobs = await _context.JobExecutions
                .Where(e => e.CreatedDateTime >= dateTimeStart && e.CreatedDateTime <= DateTimeUntil)
                .GroupBy(group => group.JobName)
                .Select(select => new ReportingJob
                {
                    SuccessPercent = select.Average(total => total.SuccessPercent),
                    JobName = select.Key
                })
                .OrderByDescending(order => order.SuccessPercent)
                .ToListAsync();


            // Get top failed steps
            var topFailedStepsGrouping = await _context.Executions
                .Where(e => e.CreatedDateTime >= dateTimeStart && e.CreatedDateTime <= DateTimeUntil)
                .ToListAsync();

            TopFailedSteps = topFailedStepsGrouping
                .GroupBy(group => new { group.StepId, group.StepName, group.JobId, group.JobName })
                .Select(select => new TopStep
                {
                    StepName = select.Key.StepName,
                    StepId = select.Key.StepId,
                    JobName = select.Key.JobName,
                    JobId = select.Key.JobId,
                    NoOfExecutions = select.Count(),
                    SuccessPercent = (decimal)select.Count(e => e.ExecutionStatus == "COMPLETED") / select.Count() * 100
                })
                .OrderBy(order => order.SuccessPercent)
                .Where(e => e.SuccessPercent < 100)
                .Take(5)
                .ToList();
        }

        public class TimeSeriesItem
        {
            public string JobName { get; set; }
            public DateTime Date { get; set; }
            public decimal DurationInMinutes { get; set; }
            public int NumberOfExecutions { get; set; }
        }

        public class ReportingJob
        {
            [DisplayFormat(DataFormatString = "{0:N0}%")]
            public decimal SuccessPercent { get; set; }
            public string JobName { get; set; }
        }

        public class TopStep
        {
            [DisplayFormat(DataFormatString = "{0:N0}%")]
            public decimal SuccessPercent { get; set; }
            public string StepName { get; set; }
            public Guid StepId { get; set; }
            public string JobName { get; set; }
            public Guid JobId { get; set; }
            public int NoOfExecutions { get; set; }
        }
    }
}