using SlateAi.Api.Features.TodaysMessage;

namespace SlateAi.Api.Tests.Features.TodaysMessage;

/// <summary>In-memory today's messages with the same rules as the real store (version-bound approval, read only once sent).</summary>
public sealed class FakeTodaysMessageStore : ITodaysMessageStore
{
    private readonly Lock _gate = new();
    private readonly HashSet<Guid> _processed = [];

    public List<TodaysMessageRow> Rows { get; } = [];

    /// <summary>How many times a draft was started (from lesson confirm or Try again).</summary>
    public int Starts { get; private set; }

    /// <summary>Whether an evening message carries it; tests wire this to the fake evening store.</summary>
    public Func<Guid, bool> IsSent { get; set; } = _ => false;

    public Task<TodaysMessageRow?> GetAsync(Guid classId, DateOnly date, CancellationToken cancellationToken) =>
        Task.FromResult(Find(classId, date));

    public Task<TodaysMessageRow?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(Rows.SingleOrDefault(r => r.Id == id) is { } row ? row with { Sent = IsSent(row.Id) } : null);
        }
    }

    public Task<Guid?> StartAsync(
        Guid classId, DateOnly date, IReadOnlyList<string> competencyIds, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            Starts++;
            var row = Rows.SingleOrDefault(r => r.ClassId == classId && r.Date == date);
            if (row is not null && IsSent(row.Id))
            {
                return Task.FromResult<Guid?>(null);
            }

            var started = (row ?? new TodaysMessageRow(Guid.NewGuid(), classId, date, [], "", 0, null, null, null, null, now, false)) with
            {
                Sections = row is not null && row.CompetencyIds.SequenceEqual(competencyIds) ? row.Sections : null,
                CompetencyIds = competencyIds, Status = TodaysMessageStatus.Generating, ApprovedVersion = null, ApprovedAt = null,
                Verdict = null, UpdatedAt = now,
            };
            Replace(row, started);
            return Task.FromResult<Guid?>(started.Id);
        }
    }

    public Task SetReadyAsync(Guid id, TodaysSections sections, string verdict, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var row = Rows.Single(r => r.Id == id);
            Replace(row, row with
            {
                Status = TodaysMessageStatus.Ready, Sections = sections, Version = row.Version + 1, Verdict = verdict,
                ApprovedVersion = null, ApprovedAt = null, UpdatedAt = now,
            });
            return Task.CompletedTask;
        }
    }

    public Task SetFailedAsync(Guid id, string verdict, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var row = Rows.Single(r => r.Id == id);
            Replace(row, row with { Status = TodaysMessageStatus.Failed, Verdict = verdict, UpdatedAt = now });
            return Task.CompletedTask;
        }
    }

    public Task<TodaysChange> ApproveAsync(
        Guid classId, DateOnly date, Guid clientRequestId, int version, Guid teacherId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (!_processed.Add(clientRequestId))
            {
                return Task.FromResult(TodaysChange.Replay);
            }

            if (Find(classId, date) is not { } row)
            {
                return Task.FromResult(TodaysChange.NotFound);
            }

            if (row.Status != TodaysMessageStatus.Ready || row.Version != version || row.Sent)
            {
                return Task.FromResult(TodaysChange.Conflict);
            }

            Replace(Rows.Single(r => r.Id == row.Id), row with { Status = TodaysMessageStatus.Approved, ApprovedVersion = version, ApprovedAt = now });
            return Task.FromResult(TodaysChange.Applied);
        }
    }

    public Task<TodaysChange> SetRemovedAsync(
        Guid classId, DateOnly date, Guid clientRequestId, string kind, bool removed, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (!_processed.Add(clientRequestId))
            {
                return Task.FromResult(TodaysChange.Replay);
            }

            if (Find(classId, date) is not { } row)
            {
                return Task.FromResult(TodaysChange.NotFound);
            }

            if (row.Status is not (TodaysMessageStatus.Ready or TodaysMessageStatus.Approved) || row.Sent
                || row.Sections?.WithRemoved(kind, removed) is not { HasAnySection: true } changed)
            {
                return Task.FromResult(TodaysChange.Conflict);
            }

            Replace(Rows.Single(r => r.Id == row.Id), row with
            {
                Sections = changed, Status = TodaysMessageStatus.Ready, Version = row.Version + 1, ApprovedVersion = null,
                ApprovedAt = null, UpdatedAt = now,
            });
            return Task.FromResult(TodaysChange.Applied);
        }
    }

    public Task<TodaysChange> ReviseAsync(
        Guid classId,
        DateOnly date,
        Guid clientRequestId,
        int version,
        Func<TodaysSections, TodaysSections?> change,
        bool redraft,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (!_processed.Add(clientRequestId))
            {
                return Task.FromResult(TodaysChange.Replay);
            }

            if (Find(classId, date) is not { } row)
            {
                return Task.FromResult(TodaysChange.NotFound);
            }

            if (row.Version != version || row.Status is not (TodaysMessageStatus.Ready or TodaysMessageStatus.Approved) || row.Sent
                || row.Sections is null || change(row.Sections) is not { } changed)
            {
                return Task.FromResult(TodaysChange.Conflict);
            }

            Replace(Rows.Single(r => r.Id == row.Id), row with
            {
                Sections = changed,
                Status = redraft ? TodaysMessageStatus.Generating : TodaysMessageStatus.Ready,
                Version = redraft ? row.Version : row.Version + 1,
                ApprovedVersion = null,
                ApprovedAt = null,
                UpdatedAt = now,
            });
            return Task.FromResult(TodaysChange.Applied);
        }
    }

    public Task<TodaysMessageRow?> GetApprovedAsync(Guid classId, DateOnly date, CancellationToken cancellationToken) =>
        Task.FromResult(Find(classId, date) is { Status: TodaysMessageStatus.Approved } row && row.ApprovedVersion == row.Version ? row : null);

    /// <summary>What the evening store's GetApprovedTodaysMessageAsync returns for this store.</summary>
    public (Guid Id, IReadOnlySet<string> Languages)? ApprovedFor(Guid classId, DateOnly date) =>
        GetApprovedAsync(classId, date, CancellationToken.None).Result is { Sections: { } sections } row
            ? (row.Id, sections.Languages.ToHashSet())
            : null;

    /// <summary>Puts a ready draft in place, as if A9 had written it.</summary>
    public TodaysMessageRow AddReady(Guid classId, DateOnly date, TodaysSections sections, int version = 1)
    {
        lock (_gate)
        {
            var row = new TodaysMessageRow(
                Guid.NewGuid(), classId, date, ["M3.BRIDGE.COMPARE"], TodaysMessageStatus.Ready, version, null, null, "valid",
                sections, DateTimeOffset.UnixEpoch, false);
            Rows.Add(row);
            return row;
        }
    }

    private TodaysMessageRow? Find(Guid classId, DateOnly date)
    {
        lock (_gate)
        {
            return Rows.SingleOrDefault(r => r.ClassId == classId && r.Date == date) is { } row ? row with { Sent = IsSent(row.Id) } : null;
        }
    }

    private void Replace(TodaysMessageRow? old, TodaysMessageRow row)
    {
        if (old is not null)
        {
            Rows.Remove(Rows.Single(r => r.Id == old.Id));
        }

        Rows.Add(row);
    }
}
