using SlateAi.Api.Features.WeeklyStar;

namespace SlateAi.Api.Tests.Features.WeeklyStar;

/// <summary>An in-memory weekly star store. <see cref="Infrastructure.FakeApi"/> registers an empty one.</summary>
public sealed class FakeWeeklyStarStore : IWeeklyStarStore
{
    private readonly Lock _gate = new();

    public string? ApprovedVersion { get; set; }
    public List<(Guid ClassId, DateOnly WeekOf, NewWeeklyStar Message)> Messages { get; } = [];

    /// <summary>What the page shows for each token: child, teacher, stopped.</summary>
    public Dictionary<Guid, (string FirstName, bool Stopped)> Children { get; } = [];

    public Task<string?> GetApprovedVersionAsync(Guid classId, CancellationToken cancellationToken) =>
        Task.FromResult(ApprovedVersion);

    public Task ApproveAsync(Guid classId, string version, Guid teacherId, CancellationToken cancellationToken)
    {
        ApprovedVersion = version;
        return Task.CompletedTask;
    }

    public Task<IReadOnlySet<Guid>> GetSentChildIdsAsync(Guid classId, DateOnly weekOf, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlySet<Guid>>(
                Messages.Where(m => m.ClassId == classId && m.WeekOf == weekOf).Select(m => m.Message.ChildId).ToHashSet());
        }
    }

    public Task<int> AddAsync(Guid classId, DateOnly weekOf, IReadOnlyList<NewWeeklyStar> messages, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var sent = Messages.Where(m => m.ClassId == classId && m.WeekOf == weekOf).Select(m => m.Message.ChildId).ToHashSet();
            var added = messages.Where(m => m.Version == ApprovedVersion && sent.Add(m.ChildId)).ToList();
            Messages.AddRange(added.Select(m => (classId, weekOf, m)));
            return Task.FromResult(added.Count);
        }
    }

    public Task<WeeklyStarPageRecord?> FindByTokenAsync(string token, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var found = Messages.Where(m => m.Message.Token == token).Select(m => ((Guid, DateOnly, NewWeeklyStar)?)m).FirstOrDefault();
            if (found is not { } row)
            {
                return Task.FromResult<WeeklyStarPageRecord?>(null);
            }

            var (_, weekOf, message) = row;
            var (name, stopped) = Children.GetValueOrDefault(message.ChildId, ("Child", false));
            return Task.FromResult<WeeklyStarPageRecord?>(new WeeklyStarPageRecord(
                name, "Shobha", message.Language, weekOf, message.MessageText, message.BadgeText, message.ExpiresAt, stopped));
        }
    }

    public Task<IReadOnlyList<SentWeeklyStar>> GetSentAsync(Guid classId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<SentWeeklyStar>>(
                Messages.Where(m => m.ClassId == classId)
                    .Select(m => new SentWeeklyStar(m.Message.ChildId, m.WeekOf, m.Message.SentAt, m.Message.Token)).ToList());
        }
    }
}
