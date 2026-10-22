using System.Threading.Channels;

namespace SlateAi.Api.Features.Lessons.Voice;

/// <summary>
/// Voice notes waiting to be matched. In memory: if the app restarts mid-way, the lesson stays "processing"
/// and reads as unclear after <see cref="LessonEndpoints.StaleProcessing"/>, so she picks from the list.
/// </summary>
public sealed class LessonMappingQueue
{
    private readonly Channel<MappingJob> _jobs = Channel.CreateBounded<MappingJob>(
        new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.DropWrite });

    public bool TryEnqueue(MappingJob job) => _jobs.Writer.TryWrite(job);

    public IAsyncEnumerable<MappingJob> ReadAllAsync(CancellationToken cancellationToken) =>
        _jobs.Reader.ReadAllAsync(cancellationToken);
}

/// <summary>Matches queued voice notes one at a time, each in its own scope, outside the upload request.</summary>
public sealed class LessonMappingWorker(
    LessonMappingQueue queue, IServiceScopeFactory scopes, ILogger<LessonMappingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in queue.ReadAllAsync(stoppingToken))
        {
            await using var scope = scopes.CreateAsyncScope();
            try
            {
                await scope.ServiceProvider.GetRequiredService<LessonMapper>().MapAsync(job, stoppingToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // Never let one note stop the worker; the lesson goes to "unclear" and she picks from the list.
                logger.LogError(e, "Lesson mapping failed");
                await scope.ServiceProvider.GetRequiredService<ILessonStore>().SaveMappingAsync(
                    job.ClassId, job.Date, job.StartedAt, A1Validator.Unclear("error:exception"), stoppingToken);
            }
        }
    }
}
