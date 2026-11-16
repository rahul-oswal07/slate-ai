using SlateAi.Api.Features.Demo;

namespace SlateAi.Api.Tests.Features.Demo;

/// <summary>The admin switch for acting as a past day (T-134), in memory: off unless a test turns it on.</summary>
public sealed class FakeSchoolDateSwitchStore(bool on = false) : ISchoolDateSwitchStore
{
    private readonly HashSet<Guid> _applied = [];

    public SchoolDateSetting Setting { get; private set; } = new(on, null);

    /// <summary>How many times a change was applied (not replayed).</summary>
    public int Writes { get; private set; }

    public Task<SchoolDateSetting> GetAsync(CancellationToken cancellationToken) => Task.FromResult(Setting);

    public Task<SchoolDateSetting> SetAsync(Guid clientRequestId, bool on, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (_applied.Add(clientRequestId))
        {
            Setting = new SchoolDateSetting(on, now);
            Writes++;
        }

        return Task.FromResult(Setting);
    }
}
