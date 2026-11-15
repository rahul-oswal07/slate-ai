using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Parent;

namespace SlateAi.Api.Tests.Features.Parent;

/// <summary>In-memory <see cref="IParentStore"/>; stop state and answers live here, as in the database.</summary>
public sealed class FakeParentStore : IParentStore
{
    private readonly Lock _gate = new();
    private readonly HashSet<Guid> _processed = [];

    /// <summary>Catch-up links by token. <c>Stopped</c>, <c>Attempts</c> and <c>ChoiceId</c> come from this store's state.</summary>
    public Dictionary<string, ParentCatchUpRecord> CatchUps { get; } = [];

    /// <summary>Evening-message links by token. <c>Stopped</c> and <c>Reason</c> are taken from this store's state.</summary>
    public Dictionary<string, EveningLink> EveningLinks { get; } = [];

    public List<AbsenceReason> Reasons { get; } = [];

    public Dictionary<Guid, DateTimeOffset> Stopped { get; } = [];

    public List<CheckResponse> Answers { get; } = [];

    public Task<ParentCatchUpRecord?> FindCatchUpAsync(string token, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(CatchUps.TryGetValue(token, out var record)
                ? record with
                {
                    Stopped = Stopped.ContainsKey(record.ChildId),
                    Attempts = Answers.SingleOrDefault(a => a.AssignmentId == record.AssignmentId)?.Attempts ?? 0,
                    ChoiceId = Answers.SingleOrDefault(a => a.AssignmentId == record.AssignmentId)?.ChoiceId,
                }
                : null);
        }
    }

    public Task<Guid?> FindChildAsync(string token, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(CatchUps.TryGetValue(token, out var record) ? record.ChildId
                : EveningLinks.TryGetValue(token, out var link) ? link.ChildId
                : (Guid?)null);
        }
    }

    public Task<int?> SaveAnswerAsync(CheckResponse response, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var stored = Answers.SingleOrDefault(a => a.AssignmentId == response.AssignmentId);
            if (!_processed.Add(response.ClientRequestId))
            {
                return Task.FromResult<int?>(stored?.Attempts ?? 0);
            }

            if (stored is null)
            {
                response.Attempts = 1;
                Answers.Add(response);
                return Task.FromResult<int?>(1);
            }

            if (stored.Attempts >= ParentRules.MaxAnswers)
            {
                _processed.Remove(response.ClientRequestId);
                return Task.FromResult<int?>(null);
            }

            response.Attempts = stored.Attempts + 1;
            Answers[Answers.IndexOf(stored)] = response;
            return Task.FromResult<int?>(response.Attempts);
        }
    }

    public Task<EveningLink?> FindEveningAsync(string token, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(EveningLinks.TryGetValue(token, out var link)
                ? link with
                {
                    Stopped = Stopped.ContainsKey(link.ChildId),
                    Reason = Reasons.SingleOrDefault(r => r.ChildId == link.ChildId && r.Date == link.Date)?.Category,
                }
                : null);
        }
    }

    public Task SaveReasonAsync(AbsenceReason reason, Guid clientRequestId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (!_processed.Add(clientRequestId))
            {
                return Task.CompletedTask;
            }

            var stored = Reasons.SingleOrDefault(r => r.ChildId == reason.ChildId && r.Date == reason.Date);
            if (stored is null)
            {
                Reasons.Add(reason);
            }
            else
            {
                stored.Category = reason.Category;
                stored.UpdatedAt = reason.UpdatedAt;
            }

            return Task.CompletedTask;
        }
    }

    public Task SetStoppedAsync(Guid childId, bool stopped, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (!stopped)
            {
                Stopped.Remove(childId);
            }
            else
            {
                Stopped.TryAdd(childId, now);
            }

            return Task.CompletedTask;
        }
    }
}
