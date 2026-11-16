using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Entities;

namespace SlateAi.Api.Features.Demo;

/// <param name="On">Whether the demo teacher can act as a past school day (T-134, D-120).</param>
/// <param name="UpdatedAt">When it was last switched on the admin page; null: never, so it is off.</param>
public sealed record SchoolDateSetting(bool On, DateTimeOffset? UpdatedAt);

/// <summary>The admin switch for acting as a past day (T-134), behind an interface so endpoint tests can use a fake.</summary>
public interface ISchoolDateSwitchStore
{
    /// <summary>The switch; off when it was never set.</summary>
    Task<SchoolDateSetting> GetAsync(CancellationToken cancellationToken);

    /// <summary>Switches it, unless <paramref name="clientRequestId"/> was already applied. Returns it afterwards.</summary>
    Task<SchoolDateSetting> SetAsync(Guid clientRequestId, bool on, DateTimeOffset now, CancellationToken cancellationToken);
}

public sealed class SchoolDateSwitchStore(AppDbContext db) : ISchoolDateSwitchStore
{
    public const string Key = "demo.school-date";
    private const string Endpoint = "admin-school-date";

    public async Task<SchoolDateSetting> GetAsync(CancellationToken cancellationToken) =>
        View(await db.AppSettings.AsNoTracking().SingleOrDefaultAsync(s => s.Key == Key, cancellationToken));

    public async Task<SchoolDateSetting> SetAsync(
        Guid clientRequestId, bool on, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock(hashtextextended({Key}, 0))", cancellationToken);
        var setting = await db.AppSettings.SingleOrDefaultAsync(s => s.Key == Key, cancellationToken);
        if (!await db.ProcessedRequests.AnyAsync(r => r.ClientRequestId == clientRequestId, cancellationToken))
        {
            if (setting is null)
            {
                setting = new AppSetting { Key = Key, Value = "" };
                db.AppSettings.Add(setting);
            }

            setting.Value = on ? "on" : "off";
            setting.UpdatedAt = now;
            db.ProcessedRequests.Add(new ProcessedRequest { ClientRequestId = clientRequestId, Endpoint = Endpoint, CreatedAt = now });
            await db.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return View(setting);
    }

    private static SchoolDateSetting View(AppSetting? setting) =>
        setting is null ? new SchoolDateSetting(false, null) : new SchoolDateSetting(setting.Value == "on", setting.UpdatedAt);
}

/// <summary>
/// The switch as the middleware and <c>/api/me</c> read it: from the store once, then from memory, updated by the admin
/// page's write (one container per server). Off until it is read.
/// </summary>
public sealed class SchoolDateSwitch
{
    private volatile StrongBox<bool>? _on;

    public async Task<bool> IsOnAsync(ISchoolDateSwitchStore store, CancellationToken cancellationToken)
    {
        if (_on is { } known)
        {
            return known.Value;
        }

        var on = (await store.GetAsync(cancellationToken)).On;
        _on = new StrongBox<bool>(on);
        return on;
    }

    /// <summary>The value the admin page just saved.</summary>
    public void Remember(bool on) => _on = new StrongBox<bool>(on);

    private sealed class StrongBox<T>(T value)
    {
        public T Value { get; } = value;
    }
}
