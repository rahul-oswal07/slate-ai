using Microsoft.Extensions.Options;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.Delivery;
using SlateAi.Api.Features.LessonAudio;
using SlateAi.Api.Features.TodaysMessage;

namespace SlateAi.Api.Features.EveningMessages;

/// <summary>
/// The 6 pm job (FR-20, and approved catch-ups, FR-08). Once a minute from <see cref="EveningOptions.SendAt"/> IST until midnight it sends each
/// class's evening messages. Sending is idempotent per child and day, so a lesson confirmed at 7 pm still goes on
/// the next tick and a restart never sends twice. A class whose ticked lesson clip awaits her approval waits while it is
/// made and then <see cref="EveningOptions.LessonAudioWait"/> for her to check it (T-82, T-85). A class whose today's
/// message is being written or awaits her approval waits the same way, for
/// <see cref="EveningOptions.TodaysMessageWait"/> (T-111, D-100).
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

    /// <summary>
    /// One tick: before the send time it only delivers what is already queued (a catch-up sent from the demo); otherwise
    /// it sends for every class, then delivers to families' phones (T-40). Returns classes processed.
    /// </summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        var classes = await SendClassesAsync(cancellationToken);
        await DeliverAsync(cancellationToken);
        return classes;
    }

    private async Task DeliverAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<DeliveryDispatcher>().RunOnceAsync(cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogError(e, "Delivery to families' phones failed");
        }
    }

    private async Task<int> SendClassesAsync(CancellationToken cancellationToken)
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
                    var lessonAudio = await classScope.ServiceProvider.GetRequiredService<ILessonAudioStore>()
                        .GetDayAsync(classId, today, cancellationToken);
                    var todays = await classScope.ServiceProvider.GetRequiredService<ITodaysMessageStore>()
                        .GetAsync(classId, today, cancellationToken);
                    var now = clock.GetUtcNow();
                    var sendAt = SchoolClock.At(today, options.Value.SendAt);
                    if (LessonAudioWait.Holds(lessonAudio, now, sendAt, options.Value.LessonAudioWait))
                    {
                        // She ticked the lesson audio and hasn't approved a clip yet: sending now would lock her out (T-82).
                        logger.LogInformation(
                            "Evening messages for class {ClassId} on {Date}: waiting for lesson audio approval", classId, today);
                    }
                    else if (TodaysMessageWait.Holds(todays, now, sendAt, options.Value.TodaysMessageWait))
                    {
                        // Today's message is being written or she is reading it: the plain message now would leave her
                        // Approve with nothing to attach to (T-111).
                        logger.LogInformation(
                            "Evening messages for class {ClassId} on {Date}: waiting for today's message approval", classId, today);
                    }
                    else
                    {
                        await classScope.ServiceProvider.GetRequiredService<EveningSender>().SendAsync(classId, today, cancellationToken);
                    }

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
