using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using EtlManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EtlManager.Pages.Executions
{
    public class JobDetailsModel : PageModel
    {
        private readonly Data.EtlManagerContext _context;
        private readonly IConfiguration _configuration;
        private readonly IAuthorizationService _authorizationService;

        public JobDetailsModel(Data.EtlManagerContext context, IConfiguration configuration, IAuthorizationService authorizationService)
        {
            _context = context;
            _configuration = configuration;
            _authorizationService = authorizationService;
        }

        public Guid ExecutionId { get; set; }

        public bool IsOperator { get; set; } = false;

        public bool Graph = false;
        public bool Collapsed = false;

        public IList<StepExecution> Executions { get; set; }

        public JobExecution JobExecution { get; set; }



        public IList<ChartElement> ChartElements = new List<ChartElement>();
        public IList<ChartLabel> ChartLabels = new List<ChartLabel>();

        public int ChartHeight;
        public int ChartHeightCollapsed = 500;

        public int ChartPaddingTop = 25;

        public int ChartPaddingBottom = 0;
        public int ChartPaddingBottomCollapsed = 20;

        public double BarHeight = 10;
        public double BarHeightCollapsed = 10;


        public int ChartWidth = 1200;
        public int ChartPaddingLeft = 250;

        public DateTime MinTime { get; set; }
        public DateTime MaxTime { get; set; }

        private const int MinWidth = 5;


        public async Task OnGetAsync(Guid id, bool graph = false, bool collapsed = false)
        {
            var authorized = await _authorizationService.AuthorizeAsync(User, "RequireOperator");
            if (authorized.Succeeded)
            {
                IsOperator = true;
            }

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

            if (JobExecution == null)
            {
                JobExecution = new JobExecution { ExecutionId = id, JobName = "Waiting for execution to start..." };
                return;
            }

            // No steps were started, no need to calculate chart properties or add chart elements.
            if (Executions.All(e => e.StartDateTime == null))
            {
                return;
            }

            // Calculate chart properties

            ChartHeight = Executions.Count * 30 + ChartPaddingTop;

            double yInterval = (double)(ChartHeight - ChartPaddingTop - ChartPaddingBottom) / Executions.Count(e => e.StartDateTime != null);
            double yIntervalCollapsed = (double)(ChartHeightCollapsed - ChartPaddingTop - ChartPaddingBottomCollapsed) / Executions.Count(e => e.StartDateTime != null);
            
            BarHeight = (double)(ChartHeight - ChartPaddingTop - ChartPaddingBottom) / Executions.Count(e => e.StartDateTime != null) / 2.0;
            BarHeightCollapsed = (double)(ChartHeightCollapsed - ChartPaddingTop - ChartPaddingBottomCollapsed) / Executions.Count(e => e.StartDateTime != null) / 2.0;

            double yLocation = 0;
            double yLocationCollapsed = 0;

            MinTime = (DateTime)Executions.Min(e => e.StartDateTime);

            // If there are uncompleted steps
            if (Executions.Any(e => e.EndDateTime == null))
            {
                MaxTime = Executions.Where(e => e.StartDateTime != null && e.ExecutionInSeconds != null)
                    .Select(e => ((DateTime)e.StartDateTime).AddSeconds((double)e.ExecutionInSeconds))
                    .Max();
            }
            else
            {
                MaxTime = (DateTime)Executions.Max(e => e.EndDateTime);
            }

            long minTicks = MinTime.Ticks;
            long maxTicks = MaxTime.Ticks;

            foreach (var step in Executions.Where(e => e.StartDateTime != null).Select(e => new KeyValuePair<Guid, string>(e.StepId, e.StepName)).Distinct())
            {
                ChartLabels.Add(new ChartLabel
                {
                    StepId = step.Key,
                    StepName = step.Value,
                    LabelYLocation = yLocation.ToString().Replace(',', '.'),
                    LabelYLocationCollapsed = yLocationCollapsed.ToString().Replace(',', '.'),
                    YLocationDouble = yLocation,
                    YLocationCollapsedDouble = yLocationCollapsed
                });
                yLocation += yInterval;
                yLocationCollapsed += yIntervalCollapsed;
            }

            foreach (var execution in Executions.Where(e => e.StartDateTime != null))
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

                var yLocation_ = ChartLabels.Where(label => label.StepId == execution.StepId).Select(label => label.YLocationDouble).First();
                var yLocationCollapsed_ = ChartLabels.Where(label => label.StepId == execution.StepId).Select(label => label.YLocationCollapsedDouble).First();

                ChartElements.Add(new ChartElement
                {
                    StepExecutionId = execution.StepExecutionId,
                    ExecutionStatus = execution.ExecutionStatus,
                    BarYLocation = (yLocation_ - BarHeight / 2).ToString().Replace(',', '.'),
                    BarYLocationCollapsed = (yLocationCollapsed_ - BarHeightCollapsed / 2).ToString().Replace(',', '.'),
                    BarXLocation = xLocation.ToString().Replace(',', '.'),
                    BarWidth = width.ToString().Replace(',', '.')
                });

            }

        }

        public async Task<JsonResult> OnPostStopJobExecution(Guid id)
        {
            var authorized = await _authorizationService.AuthorizeAsync(User, "RequireOperator");
            if (!authorized.Succeeded)
            {
                return new JsonResult(new { success = false, responseText = "Unauthorized" });
            }

            if (id == null)
            {
                return new JsonResult(new { success = false, responseText = "Id argument was null" });
            }

            try
            {
                await Utility.StopJobExecution(_configuration, id);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, responseText = "Error stopping execution: " + ex.Message });
            }

            return new JsonResult(new { success = true });
        }

        public async Task<JsonResult> OnPostStopStepExecution(Guid executionId, Guid stepId, int retryAttemptIndex)
        {
            var authorized = await _authorizationService.AuthorizeAsync(User, "RequireOperator");
            if (!authorized.Succeeded)
            {
                return new JsonResult(new { success = false, responseText = "Unauthorized" });
            }

            if (executionId == null || stepId == null)
            {
                return new JsonResult(new { success = false, responseText = "Invalid arguments" });
            }

            try
            {
                await Utility.StopStepExecution(_configuration, executionId, stepId, retryAttemptIndex);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, responseText = "Error stopping step: " + ex.Message });
            }

            return new JsonResult(new { success = true });
        }

        public class ChartElement
        {
            public string StepExecutionId { get; set; }
            public string ExecutionStatus { get; set; }
            public string BarYLocation { get; set; }
            public string BarYLocationCollapsed { get; set; }
            public string BarXLocation { get; set; }
            public string BarWidth { get; set; }
        }

        public class ChartLabel
        {
            public Guid StepId { get; set; }
            public string StepName { get; set; }
            public string LabelYLocation { get; set; }
            public string LabelYLocationCollapsed { get; set; }
            public double YLocationDouble { get; set; }
            public double YLocationCollapsedDouble { get; set; }
        }
    }
}