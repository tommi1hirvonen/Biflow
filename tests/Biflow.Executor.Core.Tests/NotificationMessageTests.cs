using Biflow.Core.Entities;
using Biflow.Executor.Core.Notification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Biflow.Executor.Core.Test;

public class NotificationMessageTests(DatabaseFixture fixture, ITestOutputHelper output)
    : IClassFixture<DatabaseFixture>
{
    private readonly INotificationMessageService _notificationMessageService =
        fixture.Services.GetRequiredService<INotificationMessageService>();
    private readonly INotificationService _notificationService =
        fixture.Services.GetRequiredService<INotificationService>();
    private readonly IDbContextFactory<ExecutorDbContext> _dbContextFactory =
        fixture.Services.GetRequiredService<IDbContextFactory<ExecutorDbContext>>();
    
    [Fact]
    public async Task BuildNotificationMessage()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var execution = await context.Executions
            .Include(e => e.StepExecutions).ThenInclude(e => e.StepExecutionAttempts)
            .FirstAsync(e => e.ExecutionStatus == ExecutionStatus.Failed);
        var notificationMessage = await _notificationMessageService.CreateMessageBodyAsync(execution);
        output.WriteLine(notificationMessage);
        Assert.NotEmpty(notificationMessage);
    }

    [Fact]
    public async Task SendNotificationAsync()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var execution = await context.Executions
            .Include(e => e.StepExecutions).ThenInclude(e => e.StepExecutionAttempts)
            .FirstAsync(e => e.ExecutionStatus == ExecutionStatus.Failed);
        await _notificationService.SendCompletionNotificationAsync(execution);
    }
}