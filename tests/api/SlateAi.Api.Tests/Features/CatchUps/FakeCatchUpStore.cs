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

    /// <summary>Each child's first name, consent and stop status (for views and the send rule).</summary>
    public Dictionary<Guid, (string FirstName, string Consent, bool Stopped)> Families { get; } = [];

    private readonly HashSet<Guid> _processed = [];
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
                .Select(p => new PackageRecord(p.Id, p.ClassId, p.WeekOf, p.TargetCompetencyIds, p.TargetKey, p.Language, p.Status, p.ScriptVersion, p.Hint,
                    Assignments.Where(a => a.PackageId == p.Id).Select(a => a.ChildId).ToList()))
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
            return Task.FromResult<IReadOnlyList<PackageView>>(Packages.Where(p => p.WeekOf == weekOf).Select(View).ToList());
        }
    }

    public Task<PackageView?> GetViewAsync(Guid packageId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(Packages.Where(p => p.Id == packageId).Select(View).FirstOrDefault());
        }
    }

    public Task<PackageChange> ApproveAsync(Guid packageId, Guid clientRequestId, int scriptVersion, Guid teacherId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_processed.Contains(clientRequestId))
            {
                return Task.FromResult(PackageChange.Replay);
            }

            var p = Packages.Single(x => x.Id == packageId);
            if (p.Status != PackageStatus.Ready || p.ScriptVersion != scriptVersion)
            {
                return Task.FromResult(PackageChange.Conflict);
            }

            _processed.Add(clientRequestId);
            p.Status = PackageStatus.Approved;
            p.ApprovedAt = Now();
            p.ApprovedByTeacherId = teacherId;
            p.ApprovedScriptVersion = scriptVersion;
            return Task.FromResult(PackageChange.Applied);
        }
    }

    public Task<PackageChange> StartRegenerateAsync(Guid packageId, Guid clientRequestId, string? hint, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_processed.Contains(clientRequestId))
            {
                return Task.FromResult(PackageChange.Replay);
            }

            var p = Packages.Single(x => x.Id == packageId);
            if (p.Status is not (PackageStatus.Ready or PackageStatus.NeedsReview or PackageStatus.Failed))
            {
                return Task.FromResult(PackageChange.Conflict);
            }

            _processed.Add(clientRequestId);
            p.Status = PackageStatus.Generating;
            p.Hint = hint;
            p.UpdatedAt = Now();
            return Task.FromResult(PackageChange.Applied);
        }
    }

    public Task<PackageChange> SendAsync(
        Guid packageId, Func<string> newToken, DateTimeOffset now, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var p = Packages.Single(x => x.Id == packageId);
            if (p.Status == PackageStatus.Sent)
            {
                return Task.FromResult(PackageChange.Replay);
            }

            if (!CatchUpSendRules.CanSend(p.Status, p.ApprovedScriptVersion, p.ScriptVersion))
            {
                return Task.FromResult(PackageChange.Conflict);
            }

            foreach (var a in Assignments.Where(a => a.PackageId == packageId && a.Token is null))
            {
                var family = Families.TryGetValue(a.ChildId, out var known) ? known : (FirstName: "", Consent: "no", Stopped: false);
                if (CatchUpSendRules.ReachesFamily(family.Consent, family.Stopped))
                {
                    a.Token = newToken();
                    a.SentAt = now;
                    a.ExpiresAt = expiresAt;
                }
            }

            p.Status = PackageStatus.Sent;
            p.SentAt = now;
            return Task.FromResult(PackageChange.Applied);
        }
    }

    public Task<IReadOnlyList<Guid>> GetApprovedUnsentAsync(Guid id, DateOnly weekOf, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<Guid>>(
                Packages.Where(p => p.WeekOf == weekOf && p.Status == PackageStatus.Approved).Select(p => p.Id).ToList());
        }
    }

    private DateTimeOffset Now() => (clock ?? TimeProvider.System).GetUtcNow();

    private PackageView View(CatchUpPackage p) => new(
        p.Id, p.Language, p.TargetCompetencyIds, p.Status, p.ScriptVersion, p.Verdict, p.Script, p.KeyWordsKn, p.GlossKn, p.GlossEn,
        JsonSerializer.Deserialize<ActivityData>(p.Activity, Json)!,
        JsonSerializer.Deserialize<CheckData>(p.CheckItem, Json)!,
        Assignments.Where(a => a.PackageId == p.Id)
            .Select(a => new AssignedChild(
                a.ChildId, a.MissedCompetencyIds, JsonSerializer.Deserialize<CompressionRationale>(a.CompressionRationale, Json)!,
                Families.GetValueOrDefault(a.ChildId).FirstName ?? "", a.Token is not null,
                Families.TryGetValue(a.ChildId, out var f) && CatchUpSendRules.ReachesFamily(f.Consent, f.Stopped)))
            .ToList(),
        p.UpdatedAt,
        p.Review is null ? null : JsonSerializer.Deserialize<PackageReview>(p.Review, Json),
        p.AudioSegments is null ? null : JsonSerializer.Deserialize<SlateAi.Api.Features.Audio.AudioSegments>(p.AudioSegments, Json),
        p.ClassId,
        p.ApprovedAt,
        p.SentAt);
}
