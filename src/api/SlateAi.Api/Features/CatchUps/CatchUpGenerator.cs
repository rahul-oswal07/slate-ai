using System.Threading.Channels;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.Audio;
using SlateAi.Api.Features.CatchUps.Compression;
using SlateAi.Api.Features.CatchUps.Practice;
using SlateAi.Api.Features.CatchUps.Script;
using SlateAi.Api.Features.CatchUps.Verify;

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

/// <summary>
/// Writes one catch-up: A3 → validator → verifier (A4), with one auto-retry carrying the verifier's reasons
/// (spec §5.1 A3). Passes → ready; a second verifier failure → needsReview with the reasons; nothing usable → failed.
/// </summary>
public sealed class CatchUpWriter(
    ICatchUpStore store, ScriptWriter writer, Verifier verifier, PackageAudio audio, CurriculumGraph curriculum, PromptLibrary prompts)
{
    public const int Attempts = 2;

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
            await store.SaveScriptAsync(
                packageId, new ScriptOutcome(PackageStatus.Failed, null, "fallback:no-practice", promptVersion, null), cancellationToken);
            return;
        }

        var (from, to) = (package.WeekOf, package.WeekOf.AddDays(6));
        var examples = await store.GetTeacherExamplesAsync(package.ClassId, from, to, package.TargetIds, cancellationToken);
        var names = await store.GetNamesAsync(package.ClassId, cancellationToken);
        var activity = Activity(problems);
        var check = Check(problems);

        ScriptOutcome? outcome = null;
        IReadOnlyList<string>? feedback = null;
        for (var attempt = 1; attempt <= Attempts; attempt++)
        {
            var input = new ScriptInput(package.Language, package.TargetIds, problems, examples, package.Hint, feedback);
            var written = await writer.WriteAsync(input, names, cancellationToken);
            if (written.Content is not { } content)
            {
                // Keep a verified-but-flawed earlier attempt rather than nothing.
                outcome = outcome?.Content is not null ? outcome : new ScriptOutcome(PackageStatus.Failed, null, written.Verdict, promptVersion, null);
                feedback = [$"output: {written.Verdict}"];
                continue;
            }

            var review = await verifier.VerifyAsync(
                new VerifyInput(package.Language, package.TargetIds, content, activity, check, names), cancellationToken);
            outcome = new ScriptOutcome(
                review.Passed ? PackageStatus.Ready : PackageStatus.NeedsReview, content, review.Verdict, promptVersion,
                new PackageReview(review.Passed, attempt, review.Checks));
            if (review.Passed)
            {
                break;
            }

            feedback = review.Problems;
        }

        if (outcome!.Content is { } words && outcome.Review is { } kept)
        {
            // Audio for anything she can listen to (ready or needs review); the real length decides ready (FR-09).
            var (segments, audioCheck) = await audio.MakeAsync(packageId, package.ClassId, package.Language, words.Script, cancellationToken);
            var checks = kept.Checks.Append(audioCheck).ToList();
            var passed = kept.Passed && audioCheck.Passed;
            outcome = outcome with
            {
                Status = passed ? PackageStatus.Ready : PackageStatus.NeedsReview,
                Verdict = passed ? outcome.Verdict : new VerifierResult(false, checks).Verdict,
                Review = kept with { Passed = passed, Checks = checks },
                Audio = segments,
            };
        }

        await store.SaveScriptAsync(packageId, outcome, cancellationToken);
    }

    private static ActivityData Activity(PackageProblems p) =>
        new(p.CompetencyId, p.Activity.Kind, p.Activity.Question, p.Activity.Numbers, p.Activity.Answer, null, null);

    private static CheckData Check(PackageProblems p) =>
        new(p.CompetencyId, p.Check.Kind, p.Check.Question, p.Check.Options, p.Check.CorrectIndex, p.Check.Answer, null);
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
                    packageId, new ScriptOutcome(PackageStatus.Failed, null, "error:exception", 0, null), stoppingToken);
            }
        }
    }
}
