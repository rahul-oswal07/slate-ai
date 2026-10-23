using System.Text.Json;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.CatchUps;

namespace SlateAi.Api.Tests.Features.CatchUps;

/// <summary>In-memory <see cref="ICatchUpStore"/>; the worker writes from a background thread, hence the lock.</summary>
/// <param name="clock">The same clock the API uses, so "stale generating" is judged on one timeline.</param>
public sealed class FakeCatchUpStore(Guid classId, TimeProvider? clock = null) : ICatchUpStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly Lock _gate = new();

    public Dictionary<Guid, string> Languages { get; } = [];
    public List<string> Names { get; } = [];
    public List<CatchUpPackage> Packages { get; } = [];
    public List<CatchUpAssignment> Assignments { get; } = [];

    public Task<IReadOnlySet<Guid>> GetAssignedChildIdsAsync(Guid id, DateOnly weekOf, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlySet<Guid>>(Assignments.Where(a => a.WeekOf == weekOf).Select(a => a.ChildId).ToHashSet());
        }
    }

    public Task<IReadOnlyDictionary<Guid, string>?> GetChildLanguagesAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, string>?>(id == classId ? Languages : null);

    public Task<IReadOnlyList<string>> GetNamesAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<string>>(Names);

    public Task<IReadOnlyList<string>> GetTeacherExamplesAsync(
        Guid id, DateOnly from, DateOnly to, IReadOnlyList<string> competencyIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<string>>(["bundles of sticks"]);

    public Task<IReadOnlyList<Guid>> SaveGenerationAsync(
        Guid id, DateOnly weekOf, IReadOnlyList<PlannedPackage> packages, IReadOnlyList<PlannedAssignment> assignments,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var created = new List<Guid>();
            foreach (var planned in packages.Where(p => !Packages.Any(s => s.WeekOf == weekOf && s.Language == p.Language && s.TargetKey == p.TargetKey)))
            {
                var package = CatchUpStore.NewPackage(id, weekOf, planned, (clock ?? TimeProvider.System).GetUtcNow());
                Packages.Add(package);
                if (package.Status == PackageStatus.Generating)
                {
                    created.Add(package.Id);
                }
            }

            foreach (var planned in assignments.Where(a => !Assignments.Any(s => s.WeekOf == weekOf && s.ChildId == a.ChildId)))
            {
                Assignments.Add(new CatchUpAssignment
                {
                    Id = Guid.NewGuid(),
                    ClassId = id,
                    ChildId = planned.ChildId,
                    PackageId = Packages.Single(p => p.WeekOf == weekOf && p.Language == planned.Language && p.TargetKey == planned.TargetKey).Id,
                    WeekOf = weekOf,
                    MissedCompetencyIds = [.. planned.MissedCompetencyIds],
                    CompressionRationale = planned.RationaleJson,
                });
            }

            return Task.FromResult<IReadOnlyList<Guid>>(created);
        }
    }

    public Task<PackageRecord?> GetPackageAsync(Guid packageId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(Packages.Where(p => p.Id == packageId)
                .Select(p => new PackageRecord(p.Id, p.ClassId, p.WeekOf, p.TargetCompetencyIds, p.TargetKey, p.Language, p.Status, p.ScriptVersion, p.Hint))
                .SingleOrDefault());
        }
    }

    public List<ScriptOutcome> Outcomes { get; } = [];

    public Task SaveScriptAsync(Guid packageId, ScriptOutcome outcome, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            Outcomes.Add(outcome);
            var p = Packages.Single(x => x.Id == packageId);
            p.ScriptVersion++;
            p.Verdict = outcome.Verdict;
            p.Status = outcome.Status;
            p.Review = outcome.Review is null ? null : JsonSerializer.Serialize(outcome.Review, Json);
            p.AudioSegments = outcome.Audio is null ? null : JsonSerializer.Serialize(outcome.Audio, Json);
            if (outcome.Content is { } c)
            {
                p.Script = c.Script;
                p.KeyWordsKn = [.. c.KeyWordsKn];
                p.GlossKn = c.GlossKn;
                p.GlossEn = c.GlossEn;
                p.CheckItem = JsonSerializer.Serialize(JsonSerializer.Deserialize<CheckData>(p.CheckItem, Json)! with { Prompt = c.CheckPrompt }, Json);
            }

            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyList<PackageView>> ListAsync(Guid id, DateOnly weekOf, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<PackageView>>(Packages.Where(p => p.WeekOf == weekOf).Select(p => new PackageView(
                p.Id, p.Language, p.TargetCompetencyIds, p.Status, p.ScriptVersion, p.Verdict, p.Script, p.KeyWordsKn, p.GlossKn, p.GlossEn,
                JsonSerializer.Deserialize<ActivityData>(p.Activity, Json)!,
                JsonSerializer.Deserialize<CheckData>(p.CheckItem, Json)!,
                Assignments.Where(a => a.PackageId == p.Id)
                    .Select(a => new AssignedChild(a.ChildId, a.MissedCompetencyIds, JsonSerializer.Deserialize<CompressionRationale>(a.CompressionRationale, Json)!))
                    .ToList(),
                p.UpdatedAt,
                p.Review is null ? null : JsonSerializer.Deserialize<PackageReview>(p.Review, Json),
                p.AudioSegments is null ? null : JsonSerializer.Deserialize<SlateAi.Api.Features.Audio.AudioSegments>(p.AudioSegments, Json))).ToList());
        }
    }
}
