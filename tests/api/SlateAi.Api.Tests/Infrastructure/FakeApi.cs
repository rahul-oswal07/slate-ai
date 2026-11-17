using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.Delivery;
using SlateAi.Api.Features.Demo;
using SlateAi.Api.Features.LessonContents;
using SlateAi.Api.Features.LessonLinks;
using SlateAi.Api.Features.WeeklyStar;
using SlateAi.Api.Tests.Curriculum;
using SlateAi.Api.Tests.Features.Delivery;
using SlateAi.Api.Tests.Features.Demo;
using SlateAi.Api.Tests.Features.LessonContents;
using SlateAi.Api.Tests.Features.LessonLinks;
using SlateAi.Api.Tests.Features.WeeklyStar;

namespace SlateAi.Api.Tests.Infrastructure;

/// <summary>The API with no database: migrations off, unreachable connection string, services replaced by fakes.</summary>
public static class FakeApi
{
    private const string NoDatabase = "Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=1";

    public static ApiFactory Create(
        Action<IServiceCollection>? services = null,
        IReadOnlyDictionary<string, string?>? settings = null,
        string? webRoot = null,
        string environment = "Testing") =>
        new(NoDatabase, initialiseDatabase: false, webRoot: webRoot, settings: settings, environment: environment, services: all =>
        {
            // Session cookies need keys; the real ones live in Postgres.
            all.AddDataProtection().UseEphemeralDataProtectionProvider();
            // The 6 pm job and the demo send run the delivery outbox (T-40); tests may replace this empty one.
            all.AddSingleton<IDeliveryStore>(new FakeDeliveryStore());
            // A new today's message reads the lesson links (T-94); none unless a test adds some.
            all.AddSingleton<ILessonLinkStore>(new FakeLessonLinkStore());
            // ... and the lessons' descriptions and examples (T-141); none unless a test adds some.
            all.AddSingleton<ILessonContentStore>(new FakeLessonContentStore());
            // The 6 pm job, the demo send and family links read the weekly star (T-125); nothing unless a test adds it.
            all.AddSingleton<IWeeklyStarStore>(new FakeWeeklyStarStore());
            // Acting as a past day (T-134) is off unless a test switches it on.
            all.AddSingleton<ISchoolDateSwitchStore>(new FakeSchoolDateSwitchStore());
            // Every class follows the built-in curriculum (T-145) unless a test gives one a stored curriculum.
            all.AddSingleton<ICurriculumStore>(new FakeCurriculumStore());
            services?.Invoke(all);
            // Nothing reads an app-wide graph any more, so one registered here would be silently ignored.
            if (all.Any(d => d.ServiceType == typeof(CurriculumGraph)))
            {
                throw new InvalidOperationException(
                    "Register `new BuiltInCurriculum(graph)` instead of a CurriculumGraph: the app reads each class's curriculum (T-145).");
            }
        });
}

/// <summary>A clock stuck at one instant.</summary>
public sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

/// <summary>A clock a test can move forward.</summary>
public sealed class MovableClock : TimeProvider
{
    public DateTimeOffset Now { get; set; }

    public override DateTimeOffset GetUtcNow() => Now;
}
