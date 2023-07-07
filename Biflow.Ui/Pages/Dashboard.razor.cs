using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Components;
using Biflow.Ui.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Biflow.Ui.Pages;

public partial class Dashboard : ComponentBase
{
    [Inject] private IDbContextFactory<BiflowContext> DbFactory { get; set; } = null!;

    private Dictionary<string, List<TimeSeriesItem>> TimeSeriesItems { get; set; } = new Dictionary<string, List<TimeSeriesItem>>();

    private Dictionary<string, string> JobColors { get; set; } = new();

    private List<ReportingJob>? Jobs { get; set; }

    private List<TopStep>? TopFailedSteps { get; set; }

    private DateTime FromDate { get; set; } = DateTime.Now.Date.AddDays(-90);

    private DateTime ToDate { get; set; } = DateTime.Now.Date.AddDays(1);

    private bool IncludeDeleted { get; set; } = false;

    private bool OnlyScheduled { get; set; } = true;

    private bool Loading { get; set; }

    private bool ReportLoaded { get; set; } = false;

    private const string ReportNotLoadedMessage = "Load report to show data.";

    private const string ReportNotLoadedClass = "text-secondary small fst-italic";

    private LineChartDataset? DurationDataset { get; set; }

    private LineChartDataset? NoOfExecutionsDataset { get; set; }

    private BarChartDataset? SuccessRateDataset { get; set; }

    private async Task LoadData()
    {
        Loading = true;

        using var context = await Task.Run(DbFactory.CreateDbContext);

        // Get duration executions
        var executionsQuery = context.Executions
            .AsNoTrackingWithIdentityResolution()
            .Where(e => e.EndDateTime != null && e.CreatedDateTime >= FromDate && e.CreatedDateTime < ToDate);

        if (!IncludeDeleted)
        {
            executionsQuery = executionsQuery.Where(e => context.Jobs.Where(job => job.JobId == e.JobId).Any());
        }

        if (OnlyScheduled)
        {
            executionsQuery = executionsQuery.Where(e => e.ScheduleId != null);
        }

        // Group job executions by job names to day level and calculate each job's average duration as well as the number of executions.
        var executions = await executionsQuery
            .Select(e => new { Execution = e, e.Job!.JobName })
            .OrderBy(e => e.Execution.CreatedDateTime).ToListAsync();
        // Replace the historized job name with the current job name if it is available.
        executions.ForEach(e => e.Execution.JobName = e.JobName is not null ? e.JobName : e.Execution.JobName);
        var executions_ = executions
            .Select(e => e.Execution)
            .GroupBy(group => new
            {
                group.JobName,
                ((DateTimeOffset)group.CreatedDateTime.LocalDateTime).Date
            })
            .Select(select => new TimeSeriesItem
            {
                DurationInMinutes = select.Average(total => (decimal)(total.ExecutionInSeconds ?? 0) / 60),
                JobName = select.Key.JobName,
                Date = select.Key.Date,
                NumberOfExecutions = select.Count()
            });

        TimeSeriesItems = executions_
            .GroupBy(e => e.JobName)
            .ToDictionary(e => e.Key, e => e.ToList());


        // Job success rates
        var jobsQuery = context.Executions
            .AsNoTrackingWithIdentityResolution()
            .Include(e => e.StepExecutions)
            .ThenInclude(e => e.StepExecutionAttempts)
            .Where(e => e.CreatedDateTime >= FromDate && e.CreatedDateTime < ToDate);

        if (!IncludeDeleted)
        {
            jobsQuery = jobsQuery.Where(e => context.Jobs.Where(job => job.JobId == e.JobId).Any());
        }

        if (OnlyScheduled)
        {
            jobsQuery = jobsQuery.Where(e => e.ScheduleId != null);
        }

        // Group job executions by job name and calculate average success percentage.
        var jobs = await jobsQuery
            .Select(e => new { Execution = e, e.Job!.JobName })
            .ToListAsync();
        // Replace the historized job name with the current job name if it is available.
        jobs.ForEach(j => j.Execution.JobName = j.JobName is not null ? j.JobName : j.Execution.JobName);
        Jobs = jobs
            .Select(j => j.Execution)
            .GroupBy(group => group.JobName)
            .Select(select => new ReportingJob
            {
                SuccessPercent = select.Average(total => total.GetSuccessPercent()),
                JobName = select.Key
            })
            .OrderByDescending(order => order.SuccessPercent)
            .ToList();

        // Get a temporary list of all job names to retrieve their colors.
        var jobNames = Jobs.Select(job => job.JobName).Concat(TimeSeriesItems.Select(tsi => tsi.Key)).Distinct().OrderBy(name => name).ToList();
        var colors = ChartColors.AsArray();
        JobColors = jobNames
            .Select((name, index) => new { Item = name, Index = index })
            .ToDictionary(elem => elem.Item, elem => colors[elem.Index % colors.Length]);


        // Get top failed steps
        var topFailedStepsQuery = context.StepExecutions
            .AsNoTrackingWithIdentityResolution()
            .Include(e => e.StepExecutionAttempts)
            .Include(e => e.Execution)
            .Where(e => e.Execution.CreatedDateTime >= FromDate && e.Execution.CreatedDateTime < ToDate);

        if (!IncludeDeleted)
        {
            topFailedStepsQuery = topFailedStepsQuery.Where(e => context.Steps.Where(step => step.StepId == e.StepId).Any());
        }

        if (OnlyScheduled)
        {
            topFailedStepsQuery = topFailedStepsQuery.Where(e => e.Execution.ScheduleId != null);
        }

        var topFailedStepsGrouping = await topFailedStepsQuery
            .Select(e => new { Execution = e, e.Execution.Job!.JobName })
            .ToListAsync();
        // Replace the historized job name with the current job name if it is available.
        topFailedStepsGrouping.ForEach(e => e.Execution.Execution.JobName = e.JobName is not null ? e.JobName : e.Execution.Execution.JobName);
        // Group step executions by step and job and calculate success percentages
        // based on the number of completed executions and the number of all executions.
        TopFailedSteps = topFailedStepsGrouping
            .Select(g => g.Execution)
            .GroupBy(group => new { group.StepId, group.StepName, group.StepType, group.Execution.JobId, group.Execution.JobName })
            .Select(select => new TopStep
            {
                StepName = select.Key.StepName,
                StepId = select.Key.StepId,
                StepType = select.Key.StepType,
                JobName = select.Key.JobName,
                JobId = select.Key.JobId ?? Guid.Empty,
                NoOfExecutions = select.Count(),
                SuccessPercent = (decimal)select
                        .Count(e =>
                            e.StepExecutionAttempts.Any(attempt =>
                                attempt.ExecutionStatus == StepExecutionStatus.Succeeded || attempt.ExecutionStatus == StepExecutionStatus.Warning))
                        / select.Count() * 100
            })
            .OrderBy(order => order.SuccessPercent)
            .Where(e => e.SuccessPercent < 100)
            .Take(5)
            .ToList();

        // Create JSON dataset objects that are passed to the JS code in site.js via JSInterop.

        var durationSeries = TimeSeriesItems
            .Select((e, index) =>
            {
                var datapoints = e.Value.Select(v => new TimeSeriesDataPoint(DateOnly.FromDateTime(v.Date), v.DurationInMinutes)).ToList();
                return new LineChartSeries(Label: e.Key, DataPoints: datapoints, Color: JobColors[e.Key]);
            })
            .ToList();
        DurationDataset = new LineChartDataset(durationSeries, "min", 0);

        var noOfExecutionsSeries = TimeSeriesItems
            .Select((e, index) =>
            {
                var datapoints = e.Value.Select(v => new TimeSeriesDataPoint(DateOnly.FromDateTime(v.Date), v.NumberOfExecutions)).ToList();
                return new LineChartSeries(Label: e.Key, DataPoints: datapoints, Color: JobColors[e.Key]);
            })
            .ToList();
        NoOfExecutionsDataset = new LineChartDataset(noOfExecutionsSeries, YMin: 0, YStepSize: 1);

        var successRateSeries = Jobs
            .Select(job => new BarChartDataPoint(job.JobName, decimal.Round(job.SuccessPercent, 2), JobColors[job.JobName]))
            .ToList();
        SuccessRateDataset = new BarChartDataset(successRateSeries, 0, 100, 10, "%", true);

        ReportLoaded = true;

        Loading = false;
    }

    public class TimeSeriesItem
    {
        public string JobName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal DurationInMinutes { get; set; }
        public int NumberOfExecutions { get; set; }
    }

    public class ReportingJob
    {
        public decimal SuccessPercent { get; set; }
        public string JobName { get; set; } = string.Empty;
    }

    public class TopStep
    {
        public decimal SuccessPercent { get; set; }
        public string StepName { get; set; } = string.Empty;
        public StepType StepType { get; set; }
        public Guid StepId { get; set; }
        public string JobName { get; set; } = string.Empty;
        public Guid JobId { get; set; }
        public int NoOfExecutions { get; set; }
    }
}
