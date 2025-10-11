using Microsoft.Extensions.Logging;

namespace Biflow.Executor.Core.Notification;

internal class NotificationMessageService(
    ILogger<NotificationMessageService> logger,
    IDbContextFactory<ExecutorDbContext> dbContextFactory)
    : INotificationMessageService
{
    public async Task<string> CreateMessageBodyAsync(Execution execution, CancellationToken cancellationToken = default)
    {
        Dictionary<Guid, StepTag[]> tags;
        try
        {
            tags = await GetStepTagsAsync(execution);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting step tags for notification");
            tags = [];
        }
        
        var statusColor = execution.ExecutionStatus switch
        {
            ExecutionStatus.Succeeded => "#00b400", // green
            ExecutionStatus.Failed => "#dc0000", // red
            _ => "#ffc800" // orange
        };

        var failedSteps = execution
            .StepExecutions
            .SelectMany(e => e.StepExecutionAttempts)
            .Where(e => e.ExecutionStatus != StepExecutionStatus.Succeeded
                    && e.ExecutionStatus != StepExecutionStatus.Warning
                    && e.ExecutionStatus != StepExecutionStatus.AwaitingRetry
                    && e.ExecutionStatus != StepExecutionStatus.Retry
                    && e.ExecutionStatus != StepExecutionStatus.Skipped)
            .Select(e => $"""
                <tr>
                    <td>{e.StepExecution.StepName}</td>
                    <td>{e.StepType}</td>
                    <td>{string.Join(", ", tags.GetValueOrDefault(e.StepId)?.Select(t => t.TagName) ?? [])}</td>
                    <td>{e.StartedOn}</td>
                    <td>{e.EndedOn}</td>
                    <td>{e.GetDurationInReadableFormat()}</td>
                    <td>{e.ExecutionStatus}</td>
                    <td>{string.Join("\n\n", e.ErrorMessages.Select(m => m.Message))}</td>
                </tr>
                """);

        var messageBody = $$"""
            <html>
                <head>
                    <style>
                        body {
                            font-family: system-ui;
                        }
                        table {
                            border-collapse: collapse;
                        }
                        th {
                            padding: 8px;
                            background-color: #ccc;
                        }
                        td {
                            padding: 8px;
                        }
                        tr:nth-child(even) {
                            background-color: #f5f5f5;
                        }
                    </style>
                </head>
                <body>
                    <h3>{{execution.JobName}}</h3>
                    <hr />
                    <table>
                        <tbody>
                            <tr>
                                <td><strong>Status:</strong></td>
                                <td><span style="color:{{statusColor}};"><strong>{{execution.ExecutionStatus}}</strong></span></td>
                            </tr>
                            <tr>
                                <td>Start time:</td>
                                <td>{{execution.StartedOn}}</td>
                            </tr>
                            <tr>
                                <td>End time:</td>
                                <td>{{execution.EndedOn}}</td>
                            </tr>
                            <tr>
                                <td>Duration:</td>
                                <td>{{execution.GetDurationInReadableFormat()}}</td>
                            </tr>
                            <tr>
                                <td>Created by:</td>
                                <td>{{execution.ScheduleName?.NullIfEmpty() ?? execution.CreatedBy}}</td>
                            </tr>
                            <tr>
                                <td>Number of steps:</td>
                                <td>{{execution.StepExecutions.Count}}</td>
                            </tr>
                        </tbody>
                    </table>
                    <h4>Failed steps</h4>
                    <table border="1" style="font-size: small;">
                        <thead>
                            <tr>
                                <th>Step name</th>
                                <th>Step type</th>
                                <th>Tags</th>
                                <th>Start time</th>
                                <th>End time</th>
                                <th>Duration</th>
                                <th>Status</th>
                                <th>Error message</th>
                            </tr>
                        </thead>
                        <tbody>
                            {{string.Join("\n", failedSteps)}}
                        </tbody>
                    </table>
                </body>
            </html>
            """;

        return messageBody;
    }
    
    private async Task<Dictionary<Guid, StepTag[]>> GetStepTagsAsync(Execution execution)
    {
        var stepIds = execution.StepExecutions.Select(x => x.StepId).ToArray();
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var tags = await dbContext.Steps
            .AsNoTrackingWithIdentityResolution()
            .Where(x => stepIds.Contains(x.StepId))
            .Include(x => x.Tags)
            .ToDictionaryAsync(
                x => x.StepId,
                x => x.Tags.OrderBy(t => t.SortOrder).ThenBy(t => t.TagName).ToArray());
        return tags;
    }
}