using Microsoft.Extensions.Options;
using SlateAi.Api.Features.Attendance;

namespace SlateAi.Api.Features.EveningMessages;

/// <summary>
/// The 6 pm job (FR-20, and approved catch-ups, FR-08). Once a minute from <see cref="EveningOptions.SendAt"/> IST until midnight it sends each
/// class's evening messages. Sending is idempotent per child and day, so a lesson confirmed at 7 pm still goes on
/// the next tick and a restart never sends twice.
/// </summary>
public sealed class EveningSendWorker(
    IServiceScopeFactory scopes,
    IOptions<EveningOptions> options,
    TimeProvider clock,
    ILogger<EveningSendWorker> logger) : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.SchedulerEnabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(Interval, clock);
        do
        {
            await RunOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <summary>One tick: does nothing before the send time, otherwise sends for every class. Returns classes processed.</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        var today = SchoolClock.Today(clock);
        if (clock.GetUtcNow() < SchoolClock.At(today, options.Value.SendAt))
        {
            return 0;
        }

        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var classIds = await scope.ServiceProvider.GetRequiredService<IEveningStore>().GetClassIdsAsync(cancellationToken);
            foreach (var classId in classIds)
            {
                // A fresh scope per class: one class's failure doesn't leave a broken DbContext for the next.
                await using var classScope = scopes.CreateAsyncScope();
                try
                {
                    await classScope.ServiceProvider.GetRequiredService<EveningSender>().SendAsync(classId, today, cancellationToken);

                    // Approved catch-ups go out at the same time (T3: "Goes after 6 pm"); unapproved ones never (guardrail 3).
                    await classScope.ServiceProvider.GetRequiredService<SlateAi.Api.Features.CatchUps.CatchUpSender>()
                        .SendApprovedAsync(classId, today, cancellationToken);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    logger.LogError(e, "Evening messages failed for class {ClassId}", classId);
                }
            }

            return classIds.Count;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogError(e, "Evening message job failed");
            return 0;
        }
    }
}
