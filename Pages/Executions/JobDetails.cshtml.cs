using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using EtlManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EtlManager.Pages.Executions
{
    public class JobDetailsModel : PageModel
    {
        private readonly Data.EtlManagerContext _context;

        public JobDetailsModel(Data.EtlManagerContext context)
        {
            _context = context;
        }

        public Guid ExecutionId { get; set; }

        public bool Graph = false;
        public bool Collapsed = false;

        public IList<StepExecution> Executions { get; set; }

        public JobExecution JobExecution { get; set; }



        public Dictionary<string, ChartElement> ChartElements = new Dictionary<string, ChartElement>();

        public int ChartHeight;
        public int ChartHeightCollapsed = 550;

        public int ChartPaddingTop = 25;

        public int ChartPaddingBottom = 0;
        public int ChartPaddingBottomCollapsed = 20;

        public double BarHeight = 10;
        public double BarHeightCollapsed = 10;


        public int ChartWidth = 1000;
        public int ChartPaddingLeft = 250;

        public DateTime MinTime { get; set; }
        public DateTime MaxTime { get; set; }

        private const int MinWidth = 5;


        public async Task OnGetAsync(Guid id, bool graph = false, bool collapsed = false)
        {
            ExecutionId = id;
            Executions = await _context.Executions
                .Where(e => e.ExecutionId == id)
                .OrderBy(execution => execution.CreatedDateTime)
                .ThenBy(execution => execution.StartDateTime)
                .ToListAsync();
            JobExecution = await _context.JobExecutions
                .FirstOrDefaultAsync(e => e.ExecutionId == id);

            Graph = graph;
            Collapsed = collapsed;

            // Calculate chart properties

            ChartHeight = Executions.Count * 40 + ChartPaddingTop;

            double yInterval = (double)(ChartHeight - ChartPaddingTop - ChartPaddingBottom) / Executions.Count;
            double yIntervalCollapsed = (double)(ChartHeightCollapsed - ChartPaddingTop - ChartPaddingBottomCollapsed) / Executions.Count;
            
            BarHeight = (double)(ChartHeight - ChartPaddingTop - ChartPaddingBottom) / Executions.Count / 2.0;
            BarHeightCollapsed = (double)(ChartHeightCollapsed - ChartPaddingTop - ChartPaddingBottomCollapsed) / Executions.Count / 2.0;

            double yLocation = 0;
            double yLocationCollapsed = 0;

            MinTime = (DateTime)Executions.Min(e => e.StartDateTime);
            if (MinTime == null) return;

            if (Executions.Any(e => e.EndDateTime == null))
            {
                MaxTime = Executions.Select(e => ((DateTime)e.StartDateTime).AddSeconds((double)e.ExecutionInSeconds)).Max();
            }
            else
            {
                MaxTime = (DateTime)Executions.Max(e => e.EndDateTime);
            }

            long minTicks = MinTime.Ticks;
            long maxTicks = MaxTime.Ticks;

            foreach (var execution in Executions)
            {

                long startTicks = ((DateTime)execution.StartDateTime).Ticks;
                double xLocation = (double)(startTicks - minTicks) / (maxTicks - minTicks) * (ChartWidth - ChartPaddingLeft); // normalize time range to the chart height

                long endTicks;
                if (execution.EndDateTime != null)
                {
                    endTicks = ((DateTime)execution.EndDateTime).Ticks;
                }
                else
                {
                    endTicks = ((DateTime)execution.StartDateTime).AddSeconds((double)execution.ExecutionInSeconds).Ticks;
                }

                double endLocation = (double)(endTicks - minTicks) / (maxTicks - minTicks) * (ChartWidth - ChartPaddingLeft); // normalize time range to the chart height
                double width = endLocation - xLocation;
                width = width >= MinWidth ? width : MinWidth; // minimum value for the width to prevent hidden bars with width = 0

                ChartElements.Add(execution.StepExecutionId, new ChartElement
                {
                    LabelYLocation = yLocation.ToString().Replace(',', '.'),
                    LabelYLocationCollapsed = yLocationCollapsed.ToString().Replace(',', '.'),
                    BarYLocation = (yLocation - BarHeight / 2).ToString().Replace(',', '.'),
                    BarYLocationCollapsed = (yLocationCollapsed - BarHeightCollapsed / 2).ToString().Replace(',', '.'),
                    BarXLocation = xLocation.ToString().Replace(',', '.'),
                    BarWidth = width.ToString().Replace(',', '.')
                });

                yLocation += yInterval;
                yLocationCollapsed += yIntervalCollapsed;
            }
        }

        public class ChartElement
        {
            public string LabelYLocation { get; set; }
            public string LabelYLocationCollapsed { get; set; }
            public string BarYLocation { get; set; }
            public string BarYLocationCollapsed { get; set; }
            public string BarXLocation { get; set; }
            public string BarWidth { get; set; }
        }
    }
}