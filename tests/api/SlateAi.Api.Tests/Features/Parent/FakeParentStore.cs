using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Parent;

namespace SlateAi.Api.Tests.Features.Parent;

/// <summary>In-memory <see cref="IParentStore"/>; stop state and answers live here, as in the database.</summary>
public sealed class FakeParentStore : IParentStore
{
    private readonly Lock _gate = new();

    /// <summary>Catch-up links by token. <c>Stopped</c> and <c>Answered</c> are taken from this store's state.</summary>
    public Dictionary<string, ParentCatchUpRecord> CatchUps { get; } = [];

    /// <summary>Evening-message links by token → child.</summary>
    public Dictionary<string, Guid> EveningTokens { get; } = [];

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
                    Answered = Answers.Any(a => a.AssignmentId == record.AssignmentId),
                }
                : null);
        }
    }

    public Task<Guid?> FindChildAsync(string token, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(CatchUps.TryGetValue(token, out var record) ? record.ChildId
                : EveningTokens.TryGetValue(token, out var child) ? child
                : (Guid?)null);
        }
    }

    public Task<bool> SaveAnswerAsync(CheckResponse response, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (Answers.Any(a => a.AssignmentId == response.AssignmentId))
            {
                return Task.FromResult(false);
            }

            Answers.Add(response);
            return Task.FromResult(true);
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
