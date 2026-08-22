using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeverMissLead.Application.Interfaces;
using NeverMissLead.Domain.Enums;

namespace NeverMissLead.Infrastructure.BackgroundServices;

/// <summary>
/// Background hosted service that polls <c>follow_up_tasks</c> where
/// <c>scheduled_for &lt;= now AND status = 'Pending'</c>.
///
/// For v1, task execution records a structured audit log and marks the task as Sent.
/// Real email/SMTP provider dispatch is slated for Week 4.5.
/// </summary>
public sealed class FollowUpPollerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<FollowUpPollerService> _logger;
    private readonly TimeSpan _interval;

    public FollowUpPollerService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<FollowUpPollerService> logger,
        TimeProvider? timeProvider = null)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;

        var intervalSec = configuration.GetValue<int?>("FollowUpPoller:IntervalSeconds")
            ?? (configuration.GetValue<int?>("FollowUpPoller:IntervalMinutes") * 60)
            ?? 300; // 5 minutes default

        _interval = TimeSpan.FromSeconds(Math.Max(1, intervalSec));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FollowUpPollerService started with interval {IntervalSec}s", _interval.TotalSeconds);

        using var timer = new PeriodicTimer(_interval, _timeProvider);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingTasksAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during follow-up task polling iteration");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken))
                break;
        }

        _logger.LogInformation("FollowUpPollerService stopped");
    }

    /// <summary>
    /// Processes all pending follow-up tasks whose scheduled time has passed.
    /// Can be called directly by unit tests or administrative triggers.
    /// </summary>
    /// <returns>The number of tasks processed and marked as Sent.</returns>
    public async Task<int> ProcessPendingTasksAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var pendingTasks = await db.FollowUpTasks
            .Where(t => t.Status == FollowUpStatus.Pending && t.ScheduledFor <= now)
            .OrderBy(t => t.ScheduledFor)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (pendingTasks.Count == 0)
            return 0;

        _logger.LogInformation("Found {Count} pending follow-up task(s) due for dispatch at {Now}", pendingTasks.Count, now);

        foreach (var task in pendingTasks)
        {
            // TODO Week 4.5: wire real email delivery (e.g. FluentEmail / Resend / Mailkit)
            _logger.LogInformation(
                "Dispatching FollowUpTask {TaskId} for Business {BusinessId} (Channel: {Channel}, LeadId: {LeadId}, ConversationId: {ConversationId})",
                task.Id,
                task.BusinessId,
                task.Channel,
                task.LeadId,
                task.ConversationId);

            task.MarkSent();
        }

        await db.SaveChangesAsync(cancellationToken);
        return pendingTasks.Count;
    }
}
