using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.EveningMessages;
using SlateAi.Api.Features.Ledger;
using SlateAi.Api.Features.Parent;
using SlateAi.Api.Features.WeeklyStar;
using SlateAi.Api.Tests.Features.EveningMessages;
using SlateAi.Api.Tests.Features.Parent;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.WeeklyStar;

/// <summary>The weekly star for families (T-125, D-118): who gets it, when, only with her approval, and the page.</summary>
public sealed class WeeklyStarTests
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly Guid ClassId = Guid.NewGuid();

    // Mon 16 – Sat 21 Nov 2026.
    private static readonly DateOnly Monday = new(2026, 11, 16);
    private static readonly DateOnly Saturday = Monday.AddDays(5);

    private readonly LedgerChildRow _asha = new(Guid.NewGuid(), 1, "Asha", "kn");
    private readonly LedgerChildRow _ravi = new(Guid.NewGuid(), 2, "Ravi", "te"); // away on Tuesday
    private readonly LedgerChildRow _kiran = new(Guid.NewGuid(), 3, "Kiran", "en", "no"); // no consent
    private readonly LedgerChildRow _deepa = new(Guid.NewGuid(), 4, "Deepa", "hi", StoppedAt: DateTimeOffset.UnixEpoch); // stopped
    private readonly LedgerChildRow _mala = new(Guid.NewGuid(), 5, "Mala", "ta");
    private readonly FakeWeeklyStarStore _store = new();
    private readonly MovableClock _clock = new();
    private LedgerData _data;

    public WeeklyStarTests()
    {
        _data = Register(6);
        _clock.Now = SchoolClock.At(Saturday, new TimeOnly(18, 0));
    }

    [Fact]
    public async Task On_Saturday_each_family_of_a_child_present_every_day_gets_one_star_with_consent_and_not_stopped()
    {
        _store.ApprovedVersion = "1";
        await using var factory = Create();

        var run = await Sender(factory).SendDueAsync(ClassId, Saturday, Ct);

        Assert.Equal(new WeeklyStarRun(Monday, null, 4, 2), run);
        Assert.Equal([_asha.Id, _mala.Id], _store.Messages.Select(m => m.Message.ChildId));
        var asha = _store.Messages[0].Message;
        Assert.Equal(("kn", "1"), (asha.Language, asha.Version));
        // The approved texts with {child} still in them: the name is filled in only when the page opens.
        Assert.StartsWith("{child} ಈ ವಾರ ಪ್ರತಿದಿನ", asha.MessageText);
        Assert.Contains("{child}", asha.BadgeText);
        Assert.Equal(22, asha.Token.Length);
        Assert.Equal(Saturday.AddDays(14), SchoolClock.DateOf(asha.ExpiresAt));
    }

    [Fact]
    public async Task Sending_again_sends_nothing_new()
    {
        _store.ApprovedVersion = "1";
        await using var factory = Create();

        await Sender(factory).SendDueAsync(ClassId, Saturday, Ct);
        _clock.Now = _clock.Now.AddMinutes(1);
        var again = await Sender(factory).SendDueAsync(ClassId, Saturday, Ct);
        var sunday = await Sender(factory).SendDueAsync(ClassId, Saturday.AddDays(1), Ct);

        Assert.Equal(0, again!.Sent);
        Assert.Equal(0, sunday!.Sent);
        Assert.Equal(2, _store.Messages.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("0")] // approved an older wording: a new version asks her again (D-49)
    public async Task Nothing_goes_without_her_approval_of_the_current_wording(string? approved)
    {
        _store.ApprovedVersion = approved;
        await using var factory = Create();

        var run = await Sender(factory).SendDueAsync(ClassId, Saturday, Ct);

        Assert.Equal(WeeklyStarBlock.NotApproved, run!.Blocked);
        Assert.Empty(_store.Messages);
    }

    [Fact]
    public async Task Saturday_waits_for_its_register_and_Sunday_sends_a_week_whose_Saturday_had_no_school()
    {
        _store.ApprovedVersion = "1";
        _data = Register(5); // Mon–Fri only
        await using var factory = Create();

        var saturday = await Sender(factory).SendDueAsync(ClassId, Saturday, Ct);
        var sunday = await Sender(factory).SendDueAsync(ClassId, Saturday.AddDays(1), Ct);

        Assert.Equal(WeeklyStarBlock.NotFinished, saturday!.Blocked);
        Assert.Equal(new WeeklyStarRun(Monday, null, 4, 2), sunday);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    public async Task Weekdays_send_nothing(int day)
    {
        _store.ApprovedVersion = "1";
        await using var factory = Create();

        Assert.Null(await Sender(factory).SendDueAsync(ClassId, Monday.AddDays(7 + day), Ct));
        Assert.Empty(_store.Messages);
    }

    [Fact]
    public async Task The_demo_sends_the_latest_finished_week_on_any_day()
    {
        _store.ApprovedVersion = "1";
        await using var factory = Create();

        var wednesday = Monday.AddDays(9);
        var run = await Sender(factory).SendLatestAsync(ClassId, wednesday, Ct);

        Assert.Equal(new WeeklyStarRun(Monday, null, 4, 2), run);
    }

    [Fact]
    public async Task She_sees_every_language_filled_with_a_child_from_her_class_and_approves_once()
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId);
        var url = $"/api/classes/{ClassId}/weekly-star";

        var before = (await client.GetFromJsonAsync<WeeklyStarWordingResponse>(url, Ct))!;
        var stale = await client.PostAsJsonAsync($"{url}/approve", new ApproveWeeklyStarRequest(Guid.NewGuid(), "0"), Ct);
        var missing = await client.PostAsJsonAsync($"{url}/approve", new ApproveWeeklyStarRequest(Guid.Empty, "1"), Ct);
        var ok = await client.PostAsJsonAsync($"{url}/approve", new ApproveWeeklyStarRequest(Guid.NewGuid(), "1"), Ct);
        var after = (await client.GetFromJsonAsync<WeeklyStarWordingResponse>(url, Ct))!;

        Assert.False(before.Approved);
        Assert.Equal(["en", "hi", "kn", "ta", "te"], before.Languages.Select(l => l.Language));
        var en = before.Languages.Single(l => l.Language == "en");
        Assert.Equal("Asha", en.Message[0].Text);
        Assert.Equal("child", en.Message[0].Slot);
        Assert.DoesNotContain(before.Languages.SelectMany(l => l.WhatsApp.Concat(l.Message).Concat(l.Badge)), p => p.Text.Contains('{'));
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.True(after.Approved);
        Assert.Equal("1", _store.ApprovedVersion);
    }

    [Fact]
    public async Task Another_teacher_can_not_see_or_approve_it()
    {
        await using var factory = Create();
        using var other = factory.CreateTeacherClient(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/classes/{ClassId}/weekly-star", Ct)).StatusCode);
        Assert.Null(_store.ApprovedVersion);
    }

    [Fact]
    public async Task The_link_opens_the_star_page_with_the_name_filled_in_and_no_numbers()
    {
        _store.ApprovedVersion = "1";
        _store.Children[_asha.Id] = ("Asha", false);
        await using var factory = Create();
        await Sender(factory).SendDueAsync(ClassId, Saturday, Ct);
        using var family = factory.CreateHttpsClient();
        var token = _store.Messages[0].Message.Token;

        var page = (await family.GetFromJsonAsync<StarPage>($"/api/p/{token}", Ct))!;

        Assert.Equal(("weeklyStar", "kn", "Asha", false), (page.Kind, page.Language, page.ChildName, page.Stopped));
        Assert.Equal("Asha ಈ ವಾರ ಪ್ರತಿದಿನ ಶಾಲೆಗೆ ಬಂದಿದ್ದಾರೆ. ಪ್ರತಿದಿನ ಶಾಲೆಗೆ ಕಳುಹಿಸಿದ್ದಕ್ಕೆ ಧನ್ಯವಾದ.", page.Message);
        Assert.Equal("Asha ಗೆ ಒಂದು ನಕ್ಷತ್ರ! ಈ ವಾರ ಪ್ರತಿದಿನ ಶಾಲೆಗೆ ಬಂದದ್ದಕ್ಕೆ.", page.Badge);
        Assert.DoesNotMatch(@"\d", page.Message + page.Badge);
    }

    [Fact]
    public async Task A_stopped_family_sees_no_star_text_and_an_old_link_is_gone()
    {
        _store.ApprovedVersion = "1";
        await using var factory = Create();
        await Sender(factory).SendDueAsync(ClassId, Saturday, Ct);
        using var family = factory.CreateHttpsClient();
        var asha = _store.Messages[0].Message;
        var mala = _store.Messages[1].Message;
        _store.Children[asha.ChildId] = ("Asha", true);

        var stopped = (await family.GetFromJsonAsync<StarPage>($"/api/p/{asha.Token}", Ct))!;
        _clock.Now = mala.ExpiresAt;
        var expired = await family.GetAsync($"/api/p/{mala.Token}", Ct);
        var unknown = await family.GetAsync($"/api/p/{new string('x', 22)}", Ct);

        Assert.True(stopped.Stopped);
        Assert.Null(stopped.Message);
        Assert.Null(stopped.Badge);
        Assert.Equal(HttpStatusCode.Gone, expired.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    private sealed record StarPage(string Kind, string Language, string ChildName, bool Stopped, string? Message, string? Badge);

    /// <summary>
    /// The week's register: everyone present on the first <paramref name="days"/> days except Ravi, away on Tuesday.
    /// </summary>
    private LedgerData Register(int days)
    {
        var children = new[] { _asha, _ravi, _kiran, _deepa, _mala };
        var rows = Enumerable.Range(0, days)
            .SelectMany(d => children.Select(c => new RegisterRow(c.Id, Monday.AddDays(d), !(c == _ravi && d == 1))))
            .ToList();
        return new LedgerData(children, rows, []);
    }

    private static WeeklyStarSender Sender(ApiFactory factory) =>
        factory.Services.CreateScope().ServiceProvider.GetRequiredService<WeeklyStarSender>();

    private ApiFactory Create() =>
        FakeApi.Create(services =>
        {
            services.AddSingleton<ILedgerStore>(new FakeLedger(this));
            services.AddSingleton<IWeeklyStarStore>(_store);
            // The family link looks at catch-ups and evening messages first.
            services.AddSingleton<IParentStore>(new FakeParentStore());
            services.AddSingleton<IEveningStore>(new FakeEveningStore(ClassId, Guid.NewGuid()));
            services.AddSingleton<TimeProvider>(_clock);
        });

    /// <summary>Reads the test's current register, so a test can change it before creating the API.</summary>
    private sealed class FakeLedger(WeeklyStarTests test) : ILedgerStore
    {
        public Task<LedgerData?> LoadAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(id == ClassId ? test._data : null);
    }
}
