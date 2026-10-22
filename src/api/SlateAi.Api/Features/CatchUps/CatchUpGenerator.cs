using System.Threading.Channels;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.CatchUps.Compression;
using SlateAi.Api.Features.CatchUps.Practice;
using SlateAi.Api.Features.CatchUps.Script;

namespace SlateAi.Api.Features.CatchUps;

/// <summary>
/// Builds the week's catch-ups (FR-06, FR-07): A2 per child who missed lessons, grouped into one catch-up per
/// (targets, language) with numbers from code; the words (A3) are written in the background.
/// </summary>
public sealed class CatchUpGenerator(
    ICatchUpStore store, Compressor compressor, CurriculumGraph curriculum, CatchUpQueue queue, ILogger<CatchUpGenerator> logger)
{
    /// <summary>Plans and stores the week up to <paramref name="date"/>, queues A3 for new catch-ups. Null if no class.</summary>
    public async Task<IReadOnlyList<PackageView>?> GenerateAsync(Guid classId, DateOnly date, CancellationToken cancellationToken)
    {
        if (await store.GetChildLanguagesAsync(classId, cancellationToken) is not { } languages)
        {
            return null;
        }

        var weekOf = CompressionPlanner.Week(date).From;
        var assigned = await store.GetAssignedChildIdsAsync(classId, weekOf, cancellationToken);
        var results = await compressor.CompressClassAsync(classId, date, cancellationToken, skip: assigned);
        var (packages, assignments) = CatchUpPlanner.Plan(classId, weekOf, results, languages, curriculum);
        var created = await store.SaveGenerationAsync(classId, weekOf, packages, assignments, cancellationToken);
        foreach (var id in created.Where(id => !queue.TryEnqueue(id)))
        {
            logger.LogWarning("Catch-up queue full; package {PackageId} stays generating", id);
        }

        logger.LogInformation(
            "Catch-ups for class {ClassId}, week of {WeekOf}: {Children} new children, {Packages} new packages",
            classId, weekOf, assignments.Count, created.Count);
        return await store.ListAsync(classId, weekOf, cancellationToken);
    }
}

/// <summary>Writes one catch-up's words with A3 and stores the result.</summary>
public sealed class CatchUpWriter(ICatchUpStore store, ScriptWriter writer, CurriculumGraph curriculum, PromptLibrary prompts)
{
    public async Task WriteAsync(Guid packageId, CancellationToken cancellationToken)
    {
        if (await store.GetPackageAsync(packageId, cancellationToken) is not { Status: PackageStatus.Generating } package)
        {
            return;
        }

        var promptVersion = prompts.Get(ScriptWriter.PromptId).Version;
        var problems = PackagePractice.For(
            package.TargetIds, curriculum,
            CatchUpPlanner.SeedKey(package.ClassId, package.WeekOf, package.TargetKey, package.Language));
        if (problems is null)
        {
            await store.SaveScriptAsync(packageId, A3Validator.Refused("fallback:no-practice"), promptVersion, cancellationToken);
            return;
        }

        var (from, to) = (package.WeekOf, package.WeekOf.AddDays(6));
        var examples = await store.GetTeacherExamplesAsync(package.ClassId, from, to, package.TargetIds, cancellationToken);
        var names = await store.GetNamesAsync(package.ClassId, cancellationToken);
        var result = await writer.WriteAsync(
            new ScriptInput(package.Language, package.TargetIds, problems, examples, package.Hint), names, cancellationToken);
        await store.SaveScriptAsync(packageId, result, promptVersion, cancellationToken);
    }
}

/// <summary>
/// Catch-ups waiting for their words. In memory: after a restart a package stays "generating" and reads as failed
/// after <see cref="CatchUpEndpoints.StaleGenerating"/>, so she can try again (T3).
/// </summary>
public sealed class CatchUpQueue
{
    private readonly Channel<Guid> _jobs = Channel.CreateBounded<Guid>(
        new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.DropWrite });

    public bool TryEnqueue(Guid packageId) => _jobs.Writer.TryWrite(packageId);

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken) => _jobs.Reader.ReadAllAsync(cancellationToken);
}

/// <summary>Writes queued catch-ups one at a time, each in its own scope, outside the generate request.</summary>
public sealed class CatchUpWorker(CatchUpQueue queue, IServiceScopeFactory scopes, ILogger<CatchUpWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var packageId in queue.ReadAllAsync(stoppingToken))
        {
            await using var scope = scopes.CreateAsyncScope();
            try
            {
                await scope.ServiceProvider.GetRequiredService<CatchUpWriter>().WriteAsync(packageId, stoppingToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // Never let one catch-up stop the worker; it shows as "Generation failed" and she can try again.
                logger.LogError(e, "Writing catch-up {PackageId} failed", packageId);
                await scope.ServiceProvider.GetRequiredService<ICatchUpStore>().SaveScriptAsync(
                    packageId, A3Validator.Refused("error:exception"), 0, stoppingToken);
            }
        }
    }
}
