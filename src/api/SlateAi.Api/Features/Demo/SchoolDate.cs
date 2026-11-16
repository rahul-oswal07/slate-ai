using System.Globalization;
using Microsoft.Extensions.Options;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Seed;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.Auth;

namespace SlateAi.Api.Features.Demo;

/// <summary>
/// The school day a request acts as (T-134, D-120). The demo teacher picks a past day in the web app, which sends it in
/// <see cref="Header"/>; for the demo class (with the demo on) the whole request then works as if that day were today.
/// Requests without it, every other class and every background job use the real day.
/// </summary>
public static class SchoolDate
{
    /// <summary>The web sends the chosen day as <c>yyyy-MM-dd</c>; the response echoes it when it was applied.</summary>
    public const string Header = "X-School-Date";

    /// <summary>How far back the demo can go.</summary>
    public const int MaxDaysBack = 120;

    private static readonly AsyncLocal<DateOnly?> Chosen = new();

    /// <summary>The day the current request acts as, or null: the real day.</summary>
    public static DateOnly? Current => Chosen.Value;

    /// <summary>Acts as <paramref name="date"/> until the returned scope is disposed.</summary>
    public static IDisposable Use(DateOnly date)
    {
        var outer = Chosen.Value;
        Chosen.Value = date;
        return new Restore(outer);
    }

    /// <summary>
    /// The day to act as, or null. Only for the demo class with the demo on and the admin switch on
    /// (<see cref="SchoolDateSwitch"/>, off until switched on); only a valid day before the real
    /// <paramref name="realToday"/> and at most <see cref="MaxDaysBack"/> days back; never on the admin pages, and
    /// never for the demo Reset, which always rebuilds the story on the real date. Pure.
    /// </summary>
    public static DateOnly? Resolve(
        string? header, string method, string path, bool demoClass, bool demoEnabled, bool switchOn, DateOnly realToday)
    {
        if (!demoEnabled || !demoClass || !switchOn
            || path.StartsWith("/api/admin", StringComparison.OrdinalIgnoreCase)
            || (HttpMethods.IsPost(method) && path.Equals("/api/demo/reset", StringComparison.OrdinalIgnoreCase))
            || !DateOnly.TryParseExact(header, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return null;
        }

        return date < realToday && date >= realToday.AddDays(-MaxDaysBack) ? date : null;
    }

    /// <summary>Reads <see cref="Header"/> after sign-in and acts as that day for the rest of the request.</summary>
    public static IApplicationBuilder UseSchoolDate(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            DateOnly? date = null;
            if (context.Request.Headers.TryGetValue(Header, out var header) && context.User.GetClassId() is { } classId)
            {
                var services = context.RequestServices;
                var demo = services.GetRequiredService<IOptions<DemoOptions>>().Value;
                var demoClass = demo.Enabled
                    && classId == await services.GetRequiredService<DemoClass>().IdAsync(context.RequestAborted);
                // Read only for the demo class: every other request never touches the switch.
                var switchOn = demoClass && await services.GetRequiredService<SchoolDateSwitch>()
                    .IsOnAsync(services.GetRequiredService<ISchoolDateSwitchStore>(), context.RequestAborted);
                var realToday = SchoolClock.Today(SchoolClockReal(services.GetRequiredService<TimeProvider>()));
                date = Resolve(
                    header, context.Request.Method, context.Request.Path, demoClass, demo.Enabled, switchOn, realToday);
            }

            if (date is not { } day)
            {
                await next();
                return;
            }

            context.Response.Headers[Header] = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            using (Use(day))
            {
                await next();
            }
        });

    private static TimeProvider SchoolClockReal(TimeProvider clock) => clock is SchoolDateClock school ? school.Real : clock;

    private sealed class Restore(DateOnly? outer) : IDisposable
    {
        public void Dispose() => Chosen.Value = outer;
    }
}

/// <summary>
/// The app's clock (T-134, D-120): the real one, except inside a request acting as a chosen day (<see cref="SchoolDate"/>),
/// where it is that day at the real time of day in IST, as D-88's DemoClock was. Timers stay real, so the 6 pm job runs
/// at 6 pm. Use <see cref="SchoolClock.RealNow"/> where time must stay real (the AI log, link expiry).
/// </summary>
public sealed class SchoolDateClock(TimeProvider real) : TimeProvider
{
    public TimeProvider Real { get; } = real;

    public override DateTimeOffset GetUtcNow()
    {
        var now = Real.GetUtcNow();
        if (SchoolDate.Current is not { } day)
        {
            return now;
        }

        return SchoolClock.At(day, TimeOnly.FromDateTime(now.ToOffset(SchoolClock.Ist).DateTime)).ToUniversalTime();
    }

    public override TimeZoneInfo LocalTimeZone => Real.LocalTimeZone;

    public override long TimestampFrequency => Real.TimestampFrequency;

    public override long GetTimestamp() => Real.GetTimestamp();

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) =>
        Real.CreateTimer(callback, state, dueTime, period);
}

/// <summary>The demo class's ID, read once from the seed file.</summary>
public sealed class DemoClass(IOptions<DatabaseOptions> database)
{
    private readonly Lazy<Task<Guid>> _id = new(async () =>
        (await DemoSeedFile.LoadAsync(database.Value.SeedFile, CancellationToken.None)).Class.Id);

    public Task<Guid> IdAsync(CancellationToken cancellationToken) => _id.Value.WaitAsync(cancellationToken);
}
