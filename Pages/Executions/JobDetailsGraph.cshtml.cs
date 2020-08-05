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
    public class JobDetailsGraphModel : PageModel
    {
        private readonly Data.EtlManagerContext _context;

        public JobDetailsGraphModel(Data.EtlManagerContext context)
        {
            _context = context;
        }

        public IList<StepExecution> Executions { get; set; }

        public Dictionary<string, ChartElement> ChartElements = new Dictionary<string, ChartElement>();

        public int ChartHeight = 700;
        public int ChartWidth = 1000;
        public int ChartPaddingLeft = 250;
        public int ChartPaddingTop = 50;
        public double BarHeight { get; set; } = 10;

        public DateTime MinTime { get; set; }
        public DateTime MaxTime { get; set; }

        public async Task OnGetAsync(Guid id)
        {
            Executions = await _context.Executions
                .Where(e => e.ExecutionId == id)
                .Where(e => e.StartDateTime != null)
                .OrderBy(execution => execution.CreatedDateTime)
                .ThenBy(execution => execution.StartDateTime)
                .ToListAsync();

            ChartHeight = Executions.Count * 40 + ChartPaddingTop;

            double yInterval = (double)(ChartHeight - ChartPaddingTop) / Executions.Count;
            BarHeight = (double)(ChartHeight - ChartPaddingTop) / Executions.Count / 2.0;
            double yLocation = 0;

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
                width = width >= 10 ? width : 10; // minimum value for the width to prevent hidden bars with width = 0

                ChartElements.Add(execution.StepExecutionId, new ChartElement{
                    LabelYLocation = yLocation.ToString().Replace(',', '.'),
                    BarYLocation = (yLocation - BarHeight / 2).ToString().Replace(',', '.'),
                    BarXLocation = xLocation.ToString().Replace(',', '.'),
                    BarWidth = width.ToString().Replace(',', '.')
                });

                yLocation += yInterval;
            }
        }

        public class ChartElement
        {
            public string LabelYLocation { get; set; }
            public string BarYLocation { get; set; }
            public string BarXLocation { get; set; }
            public string BarWidth { get; set; }
        }

    }
}