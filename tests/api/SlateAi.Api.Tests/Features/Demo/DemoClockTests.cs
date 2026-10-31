using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.Demo;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.Demo;

/// <summary>A fixed demo date (T-75, D-88): the date stays, the time of day is real, the session cookie is not moved.</summary>
public sealed class DemoClockTests
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly DateOnly Saturday = new(2026, 10, 31);

    [Fact]
    public void Keeps_the_real_time_of_day_in_IST_on_the_fixed_date()
    {
        var real = new MovableClock { Now = SchoolClock.At(new DateOnly(2026, 11, 12), new TimeOnly(18, 5)) };
        var clock = new DemoClock(Saturday, real);

        Assert.Equal(SchoolClock.At(Saturday, new TimeOnly(18, 5)), clock.GetUtcNow());
        Assert.Equal(TimeSpan.Zero, clock.GetUtcNow().Offset);
        Assert.Equal(Saturday, SchoolClock.Today(clock));
    }

    [Fact]
    public void Is_still_the_fixed_date_on_the_next_real_day_and_just_after_midnight()
    {
        var real = new MovableClock { Now = SchoolClock.At(new DateOnly(2026, 11, 2), new TimeOnly(23, 59)) };
        var clock = new DemoClock(Saturday, real);
        Assert.Equal(Saturday, SchoolClock.Today(clock));

        real.Now = SchoolClock.At(new DateOnly(2026, 11, 3), new TimeOnly(0, 1));
        Assert.Equal(SchoolClock.At(Saturday, new TimeOnly(0, 1)), clock.GetUtcNow());
    }

    [Fact]
    public async Task With_Demo_Today_the_app_uses_the_demo_clock_and_says_the_date_on_every_response()
    {
        await using var factory = FakeApi.Create(settings: new Dictionary<string, string?> { ["Demo:Today"] = "2026-10-31" });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/me", Ct);

        Assert.Equal("2026-10-31", Assert.Single(response.Headers.GetValues(DemoClock.Header)));
        Assert.Equal(Saturday, Assert.IsType<DemoClock>(factory.Services.GetRequiredService<TimeProvider>()).Today);
    }

    [Fact]
    public async Task Without_it_the_clock_is_real_and_there_is_no_header()
    {
        await using var factory = FakeApi.Create();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/me", Ct);

        Assert.False(response.Headers.Contains(DemoClock.Header));
        Assert.Same(TimeProvider.System, factory.Services.GetRequiredService<TimeProvider>());
    }

    [Fact]
    public async Task Not_while_the_demo_is_off()
    {
        await using var factory = FakeApi.Create(settings: new Dictionary<string, string?>
        {
            ["Demo:Today"] = "2026-10-31",
            ["Demo:Enabled"] = "false",
        });

        Assert.Same(TimeProvider.System, factory.Services.GetRequiredService<TimeProvider>());
    }

    [Fact]
    public async Task The_session_cookie_keeps_the_real_clock()
    {
        await using var factory = FakeApi.Create(settings: new Dictionary<string, string?> { ["Demo:Today"] = "2026-10-31" });

        var cookie = factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        Assert.Same(TimeProvider.System, cookie.TimeProvider);
    }
}
