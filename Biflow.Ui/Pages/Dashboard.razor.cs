using Biflow.DataAccess;
using Biflow.DataAccess.Models;
using Biflow.Ui.Components;
using Biflow.Ui.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Biflow.Ui.Pages;

public partial class Dashboard : ComponentBase
{
    [Inject] private IDbContextFactory<AppDbContext> DbFactory { get; set; } = null!;

    private Dictionary<string, TimeSeriesItem[]> timeSeriesItems = [];
    private Dictionary<string, string> jobColors = [];
    private ReportingJob[]? jobs;
    private TopStep[]? topFailedSteps;
    private DateTime fromDate = DateTime.Now.Date.AddDays(-90);
    private DateTime toDate = DateTime.Now.Date.AddDays(1);
    private bool includeDeleted = false;
    private bool onlyScheduled = true;
    private bool loading;
    private bool reportLoaded = false;
    private LineChartDataset? durationDataset;
    private LineChartDataset? noOfExecutionsDataset;
    private BarChartDataset? successRateDataset;

    private const string ReportNotLoadedMessage = "Load report to show data.";
    private const string ReportNotLoadedClass = "text-secondary small fst-italic";

    private async Task LoadData()
    {
        loading = true;

        using var context = await Task.Run(DbFactory.CreateDbContext);

        // Get duration executions
        var executionsQuery = context.Executions
            .AsNoTrackingWithIdentityResolution()
            .Where(e => e.EndDateTime != null && e.CreatedDateTime >= fromDate && e.CreatedDateTime < toDate);

        if (!includeDeleted)
        {
            executionsQuery = executionsQuery.Where(e => context.Jobs.Where(job => job.JobId == e.JobId).Any());
        }

        if (onlyScheduled)
        {
            executionsQuery = executionsQuery.Where(e => e.ScheduleId != null);
        }

        // Group job executions by job names to day level and calculate each job's average duration as well as the number of executions.
        var executions = await executionsQuery
            .Select(e => new { Execution = e, e.Job!.JobName })
            .OrderBy(e => e.Execution.CreatedDateTime)
            .ToArrayAsync();
        var executions_ = executions
            .GroupBy(group => new
            {
                JobName = group.JobName ?? group.Execution.JobName,
                ((DateTimeOffset)group.Execution.CreatedDateTime.LocalDateTime).Date
            })
            .Select(select => new TimeSeriesItem
            {
                DurationInMinutes = select.Average(total => (decimal)(total.Execution.ExecutionInSeconds ?? 0) / 60),
                JobName = select.Key.JobName,
                Date = select.Key.Date,
                NumberOfExecutions = select.Count()
            });

        timeSeriesItems = executions_
            .GroupBy(e => e.JobName)
            .ToDictionary(e => e.Key, e => e.ToArray());


        // Job success rates
        var jobsQuery = context.Executions
            .AsNoTrackingWithIdentityResolution()
            .Include(e => e.StepExecutions)
            .ThenInclude(e => e.StepExecutionAttempts)
            .Where(e => e.CreatedDateTime >= fromDate && e.CreatedDateTime < toDate);

        if (!includeDeleted)
        {
            jobsQuery = jobsQuery.Where(e => context.Jobs.Where(job => job.JobId == e.JobId).Any());
        }

        if (onlyScheduled)
        {
            jobsQuery = jobsQuery.Where(e => e.ScheduleId != null);
        }

        // Group job executions by job name and calculate average success percentage.
        var jobs = await jobsQuery
            .Select(e => new { Execution = e, e.Job!.JobName })
            .ToArrayAsync();
        this.jobs = jobs
            .GroupBy(group => group.JobName ?? group.Execution.JobName)
            .Select(select => new ReportingJob
            {
                SuccessPercent = select.Average(total => total.Execution.GetSuccessPercent()),
                JobName = select.Key
            })
            .OrderByDescending(order => order.SuccessPercent)
            .ToArray();

        // Get a temporary list of all job names to retrieve their colors.
        var jobNames = jobs
            .Select(job => job.JobName)
            .Concat(timeSeriesItems.Select(tsi => tsi.Key))
            .Distinct()
            .OrderBy(name => name)
            .ToArray();
        var colors = ChartColors.AsArray();
        jobColors = jobNames
            .Select((name, index) => new { Item = name, Index = index })
            .ToDictionary(elem => elem.Item, elem => colors[elem.Index % colors.Length]);


        // Get top failed steps
        var topFailedStepsQuery = context.StepExecutions
            .AsNoTrackingWithIdentityResolution()
            .Include(e => e.StepExecutionAttempts)
            .Include(e => e.Execution)
            .Where(e => e.Execution.CreatedDateTime >= fromDate && e.Execution.CreatedDateTime < toDate);

        if (!includeDeleted)
        {
            topFailedStepsQuery = topFailedStepsQuery.Where(e => context.Steps.Where(step => step.StepId == e.StepId).Any());
        }

        if (onlyScheduled)
        {
            topFailedStepsQuery = topFailedStepsQuery.Where(e => e.Execution.ScheduleId != null);
        }

        var topFailedStepsGrouping = await topFailedStepsQuery
            .Select(e => new { Execution = e, e.Execution.Job!.JobName })
            .ToArrayAsync();
        // Group step executions by step and job and calculate success percentages
        // based on the number of completed executions and the number of all executions.
        topFailedSteps = topFailedStepsGrouping
            .GroupBy(group => new
            {
                group.Execution.StepId,
                group.Execution.StepName,
                group.Execution.StepType,
                group.Execution.Execution.JobId,
                JobName = group.JobName ?? group.Execution.Execution.JobName
            })
            .Select(select => new TopStep
            {
                StepName = select.Key.StepName,
                StepId = select.Key.StepId,
                StepType = select.Key.StepType,
                JobName = select.Key.JobName,
                JobId = select.Key.JobId,
                NoOfExecutions = select.Count(),
                SuccessPercent = (decimal)select
                        .Count(e =>
                            e.Execution.StepExecutionAttempts.Any(attempt =>
                                attempt.ExecutionStatus == StepExecutionStatus.Succeeded || attempt.ExecutionStatus == StepExecutionStatus.Warning))
                        / select.Count() * 100
            })
            .OrderBy(order => order.SuccessPercent)
            .Where(e => e.SuccessPercent < 100)
            .Take(5)
            .ToArray();

        // Create JSON dataset objects that are passed to the JS code in site.js via JSInterop.

        var durationSeries = timeSeriesItems
            .Select((e, index) =>
            {
                var datapoints = e.Value.Select(v => new TimeSeriesDataPoint(DateOnly.FromDateTime(v.Date), v.DurationInMinutes)).ToArray();
                return new LineChartSeries(Label: e.Key, DataPoints: datapoints, Color: jobColors[e.Key]);
            })
            .ToArray();
        durationDataset = new LineChartDataset(durationSeries, "min", 0);

        var noOfExecutionsSeries = timeSeriesItems
            .Select((e, index) =>
            {
                var datapoints = e.Value.Select(v => new TimeSeriesDataPoint(DateOnly.FromDateTime(v.Date), v.NumberOfExecutions)).ToArray();
                return new LineChartSeries(Label: e.Key, DataPoints: datapoints, Color: jobColors[e.Key]);
            })
            .ToArray();
        noOfExecutionsDataset = new LineChartDataset(noOfExecutionsSeries, YMin: 0, YStepSize: 1);

        var successRateSeries = this.jobs
            .Select(job => new BarChartDataPoint(job.JobName, decimal.Round(job.SuccessPercent, 2), jobColors[job.JobName]))
            .ToArray();
        successRateDataset = new BarChartDataset(successRateSeries, 0, 100, 10, "%", true);

        reportLoaded = true;

        loading = false;
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
