using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.CatchUps;
using SlateAi.Api.Features.CatchUps.Compression;
using SlateAi.Api.Features.CatchUps.Practice;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.CatchUps;

public sealed class CatchUpApprovalEndpointTests
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly DateOnly Friday = new(2026, 10, 23);
    private static readonly Guid Ravi = Guid.NewGuid();
    private static readonly Guid NoForm = Guid.NewGuid();
    private static readonly Guid Stopped = Guid.NewGuid();

    private readonly MovableClock _clock = new() { Now = SchoolClock.At(Friday, new TimeOnly(15, 0)) };
    private readonly FakeCatchUpStore _store;
    private readonly Guid _packageId;

    public CatchUpApprovalEndpointTests()
    {
        _store = new FakeCatchUpStore(ClassId, _clock);
        _store.Families[Ravi] = ("Ravi", "yes", false);
        _store.Families[NoForm] = ("Bhavana", "no", false);
        _store.Families[Stopped] = ("Esha", "yes", true);
        var weekOf = CompressionPlanner.Week(Friday).From;
        var problems = PackagePractice.For(["M3.SUB.BORROW"], A3ValidatorTests.Book, "seed");
        var rationale = """{"targets":[{"id":"M3.SUB.BORROW","reason":"Next week needs it."}],"dropped":[],"source":"ai","verdict":"valid"}""";
        _store.SaveGenerationAsync(ClassId, weekOf,
            [new PlannedPackage(["M3.SUB.BORROW"], "M3.SUB.BORROW", "te", problems)],
            new[] { Ravi, NoForm, Stopped }.Select(c => new PlannedAssignment(c, "M3.SUB.BORROW", "te", ["M3.SUB.BORROW"], rationale)).ToList(),
            Ct).GetAwaiter().GetResult();
        var package = _store.Packages.Single();
        package.Status = PackageStatus.Ready;
        package.ScriptVersion = 2;
        _packageId = package.Id;
    }

    [Fact]
    public async Task Approving_the_version_she_checked_approves_it()
    {
        var response = await PostAsync("approve", new ApprovePackageRequest(Guid.NewGuid(), 2));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var view = (await response.Content.ReadFromJsonAsync<PackageView>(Ct))!;
        Assert.Equal(PackageStatus.Approved, view.Status);
        Assert.NotNull(view.ApprovedAt);
        Assert.Equal(TeacherId, _store.Packages.Single().ApprovedByTeacherId);
        Assert.Equal(["Ravi", "Bhavana", "Esha"], view.Children.Select(c => c.FirstName));
        Assert.Equal(1, view.FamiliesReachable); // Bhavana has no consent; Esha's family stopped
    }

    [Fact]
    public async Task An_older_version_is_409_with_the_latest()
    {
        var response = await PostAsync("approve", new ApprovePackageRequest(Guid.NewGuid(), 1));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var latest = (await response.Content.ReadFromJsonAsync<PackageView>(Ct))!;
        Assert.Equal(2, latest.ScriptVersion);
        Assert.Equal(PackageStatus.Ready, latest.Status);
    }

    [Theory]
    [InlineData(PackageStatus.NeedsReview)]
    [InlineData(PackageStatus.Generating)]
    [InlineData(PackageStatus.Failed)]
    public async Task Only_a_ready_catch_up_can_be_approved(string status)
    {
        _store.Packages.Single().Status = status;

        var response = await PostAsync("approve", new ApprovePackageRequest(Guid.NewGuid(), 2));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(status, _store.Packages.Single().Status);
    }

    [Fact]
    public async Task Replaying_an_approval_is_a_no_op()
    {
        var id = Guid.NewGuid();
        await PostAsync("approve", new ApprovePackageRequest(id, 2));

        var again = await PostAsync("approve", new ApprovePackageRequest(id, 2));

        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
    }

    [Fact]
    public async Task Simpler_starts_a_new_version_and_queues_it_and_an_approved_one_cannot_be_regenerated()
    {
        var queue = new CatchUpQueue();
        var response = await PostAsync("regenerate", new RegeneratePackageRequest(Guid.NewGuid(), "simpler"), queue);

        // The response shows generating; the background writer may already have picked the job up since.
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(PackageStatus.Generating, (await response.Content.ReadFromJsonAsync<PackageView>(Ct))!.Status);
        var package = _store.Packages.Single();
        Assert.Equal("simpler", package.Hint);

        package.Status = PackageStatus.Approved;
        Assert.Equal(HttpStatusCode.Conflict,
            (await PostAsync("regenerate", new RegeneratePackageRequest(Guid.NewGuid(), "shorter"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await PostAsync("regenerate", new RegeneratePackageRequest(Guid.NewGuid(), "funnier"))).StatusCode);
    }

    [Fact]
    public async Task Sending_an_unapproved_catch_up_is_409_and_nothing_goes_out()
    {
        var response = await PostAsync("send", new SendPackageRequest(Guid.NewGuid()), demo: true);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.All(_store.Assignments, a => Assert.Null(a.Token));
        Assert.Equal(PackageStatus.Ready, _store.Packages.Single().Status);
    }

    [Fact]
    public async Task An_approved_catch_up_whose_version_changed_is_not_sent()
    {
        var package = _store.Packages.Single();
        package.Status = PackageStatus.Approved;
        package.ApprovedScriptVersion = 1; // approved v1, but the stored words are v2

        Assert.Equal(HttpStatusCode.Conflict, (await PostAsync("send", new SendPackageRequest(Guid.NewGuid()), demo: true)).StatusCode);
        Assert.All(_store.Assignments, a => Assert.Null(a.Token));
    }

    [Fact]
    public async Task Approved_and_sent_reaches_only_families_with_consent_who_have_not_stopped()
    {
        await PostAsync("approve", new ApprovePackageRequest(Guid.NewGuid(), 2));

        var response = await PostAsync("send", new SendPackageRequest(Guid.NewGuid()), demo: true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var view = (await response.Content.ReadFromJsonAsync<PackageView>(Ct))!;
        Assert.Equal(PackageStatus.Sent, view.Status);
        Assert.Equal(1, view.FamiliesSent);
        var ravi = _store.Assignments.Single(a => a.ChildId == Ravi);
        Assert.Equal(22, ravi.Token!.Length);
        Assert.Equal(ravi.SentAt!.Value.AddDays(14), ravi.ExpiresAt);
        Assert.Null(_store.Assignments.Single(a => a.ChildId == NoForm).Token);
        Assert.Null(_store.Assignments.Single(a => a.ChildId == Stopped).Token);

        var again = await PostAsync("send", new SendPackageRequest(Guid.NewGuid()), demo: true);
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal(ravi.Token, _store.Assignments.Single(a => a.ChildId == Ravi).Token);
    }

    [Fact]
    public async Task Before_6_pm_sending_waits_unless_the_demo_is_on()
    {
        await PostAsync("approve", new ApprovePackageRequest(Guid.NewGuid(), 2));

        Assert.Equal(HttpStatusCode.Conflict, (await PostAsync("send", new SendPackageRequest(Guid.NewGuid()), demo: false)).StatusCode);
        _clock.Now = SchoolClock.At(Friday, new TimeOnly(18, 5));
        Assert.Equal(HttpStatusCode.OK, (await PostAsync("send", new SendPackageRequest(Guid.NewGuid()), demo: false)).StatusCode);
    }

    [Fact]
    public async Task The_6_pm_job_sends_approved_catch_ups_and_never_unapproved_ones()
    {
        var sender = new CatchUpSender(_store, _clock, NullLogger<CatchUpSender>.Instance);
        Assert.Equal(0, await sender.SendApprovedAsync(ClassId, Friday, Ct));

        _store.Packages.Single().Status = PackageStatus.Approved;
        _store.Packages.Single().ApprovedScriptVersion = 2;

        Assert.Equal(1, await sender.SendApprovedAsync(ClassId, Friday, Ct));
        Assert.Equal(PackageStatus.Sent, _store.Packages.Single().Status);
    }

    [Fact]
    public async Task Another_class_gets_404_and_signed_out_gets_401()
    {
        await using var factory = Create(new CatchUpQueue(), demo: true);
        using var other = factory.CreateTeacherClient(Guid.NewGuid());
        using var anonymous = factory.CreateHttpsClient();

        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/packages/{_packageId}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await other.PostAsJsonAsync($"/api/packages/{_packageId}/approve", new ApprovePackageRequest(Guid.NewGuid(), 2), Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anonymous.PostAsJsonAsync($"/api/packages/{_packageId}/send", new SendPackageRequest(Guid.NewGuid()), Ct)).StatusCode);
    }

    private async Task<HttpResponseMessage> PostAsync<T>(string action, T body, CatchUpQueue? queue = null, bool demo = true)
    {
        await using var factory = Create(queue ?? new CatchUpQueue(), demo);
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);
        return await client.PostAsJsonAsync($"/api/packages/{_packageId}/{action}", body, Ct);
    }

    private ApiFactory Create(CatchUpQueue queue, bool demo) =>
        FakeApi.Create(
            services =>
            {
                services.AddSingleton<ICatchUpStore>(_store);
                services.AddSingleton(queue);
                services.AddSingleton<TimeProvider>(_clock);
            },
            settings: new Dictionary<string, string?> { ["Demo:Enabled"] = demo.ToString() });
}
