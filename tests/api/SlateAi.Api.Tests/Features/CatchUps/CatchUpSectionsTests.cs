using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.CatchUps;
using SlateAi.Api.Features.CatchUps.Compression;
using SlateAi.Api.Features.CatchUps.Practice;
using SlateAi.Api.Features.CatchUps.Verify;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.CatchUps;

/// <summary>
/// T-103: a catch-up's sections and her edits, tested as today's message's (T-89): every save is a new version that clears
/// her approval (guardrail 3); her words go through code checks; numbers stay the catch-up's own (guardrail 4).
/// </summary>
public sealed class CatchUpSectionsTests
{
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly Guid ClassId = Guid.NewGuid();
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly DateOnly Friday = new(2026, 10, 23);
    private static readonly Guid Ravi = Guid.NewGuid();

    private readonly MovableClock _clock = new() { Now = SchoolClock.At(Friday, new TimeOnly(15, 0)) };
    private readonly FakeCatchUpStore _store;
    private readonly Guid _packageId;
    private readonly PackageProblems _problems;

    public CatchUpSectionsTests()
    {
        _store = new FakeCatchUpStore(ClassId, _clock);
        _store.Families[Ravi] = ("Ravi", "yes", false);
        _store.Names.AddRange(["Ravi", "Shobha"]);
        var weekOf = CompressionPlanner.Week(Friday).From;
        _problems = PackagePractice.For(["M3.SUB.BORROW"], A3ValidatorTests.Book, "seed")!;
        var rationale = """{"targets":[{"id":"M3.SUB.BORROW","reason":"Next week needs it."}],"dropped":[],"source":"ai","verdict":"valid"}""";
        _store.SaveGenerationAsync(ClassId, weekOf,
            [new PlannedPackage(["M3.SUB.BORROW"], "M3.SUB.BORROW", "te", _problems)],
            [new PlannedAssignment(Ravi, "M3.SUB.BORROW", "te", ["M3.SUB.BORROW"], rationale)],
            Ct).GetAwaiter().GetResult();
        var package = _store.Packages.Single();
        package.Status = PackageStatus.Ready;
        package.ScriptVersion = 2;
        package.Script = $"ఈ రోజు {A} నుండి {B} తీసివేద్దాం. ಕಳೆ ಕಳೆ.";
        package.GlossKn = "ಕಳೆಯುವುದು.";
        package.GlossEn = "Subtracting.";
        package.Activity = JsonSerializer.Serialize(
            JsonSerializer.Deserialize<ActivityData>(package.Activity, Json)! with { Instruction = $"{A} పుల్లలు తీసుకోండి.", ObjectOption = "రాళ్లు కూడా సరే." },
            Json);
        package.Review = JsonSerializer.Serialize(new PackageReview(true, 1,
        [
            new VerifierCheck("numbers", true, "ok"), new VerifierCheck("keyWords", true, "ok"), new VerifierCheck("objects", true, "ok"),
            new VerifierCheck("content", true, "ok"), new VerifierCheck("audio", true, "55 seconds"),
        ]), Json);
        _packageId = package.Id;
    }

    private string A => _problems.Activity.Numbers[0].ToString(CultureInfo.InvariantCulture);

    private string B => _problems.Activity.Numbers[1].ToString(CultureInfo.InvariantCulture);

    [Fact]
    public async Task Her_note_is_a_new_version_that_clears_her_approval_and_nothing_old_can_go()
    {
        var package = _store.Packages.Single();
        await SendAsync(HttpMethod.Post, "approve", new ApprovePackageRequest(Guid.NewGuid(), 2));
        Assert.Equal((PackageStatus.Approved, 2), (package.Status, package.ApprovedScriptVersion));

        var response = await SendAsync(HttpMethod.Put, "sections/note", new EditPackageSectionRequest(Guid.NewGuid(), 2, "ఆకులపై సంఖ్యలు రాశాం."));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var view = (await response.Content.ReadFromJsonAsync<PackageView>(Ct))!;
        Assert.Equal((PackageStatus.Ready, 3), (view.Status, view.ScriptVersion));
        Assert.Equal(new CatchUpNote("ఆకులపై సంఖ్యలు రాశాం.", false), view.Note);
        Assert.Null(view.ApprovedAt);
        Assert.Null(package.ApprovedScriptVersion);
        Assert.Equal(HttpStatusCode.Conflict, (await SendAsync(HttpMethod.Post, "send", new SendPackageRequest(Guid.NewGuid()))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await SendAsync(HttpMethod.Post, "approve", new ApprovePackageRequest(Guid.NewGuid(), 2))).StatusCode);
        Assert.All(_store.Assignments, a => Assert.Null(a.Token));

        var again = await SendAsync(HttpMethod.Post, "approve", new ApprovePackageRequest(Guid.NewGuid(), 3, Send: true));
        Assert.Equal(PackageStatus.Sent, (await again.Content.ReadFromJsonAsync<PackageView>(Ct))!.Status);
    }

    [Fact]
    public async Task Her_script_goes_back_to_generating_to_be_voiced_and_the_old_audio_and_summary_go()
    {
        var mine = $"మీ బిడ్డతో {A} పుల్లలు లెక్కించండి. తర్వాత {B} తీసివేయండి.";

        var response = await SendAsync(HttpMethod.Put, "sections/concept", new EditPackageSectionRequest(Guid.NewGuid(), 2, mine));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var view = (await response.Content.ReadFromJsonAsync<PackageView>(Ct))!;
        Assert.Equal((PackageStatus.Generating, 3, true), (view.Status, view.ScriptVersion, view.Edited));
        Assert.Equal(mine, view.Script);
        Assert.Null(view.Audio);
        Assert.Null(view.GlossKn);
        Assert.DoesNotContain(view.Review!.Checks, c => c.Name is "content" or "keyWords" or "audio");
        Assert.Contains(view.Review.Checks, c => c is { Name: "objects", Passed: true });
    }

    [Fact]
    public async Task Her_activity_is_saved_and_ready_again_at_the_next_version()
    {
        var response = await SendAsync(HttpMethod.Put, "sections/activity",
            new EditPackageSectionRequest(Guid.NewGuid(), 2, $"{A} గింజలు పెట్టండి.", ""));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var view = (await response.Content.ReadFromJsonAsync<PackageView>(Ct))!;
        Assert.Equal((PackageStatus.Ready, 3, true), (view.Status, view.ScriptVersion, view.Edited));
        Assert.Equal($"{A} గింజలు పెట్టండి.", view.Activity.Instruction);
        Assert.Null(view.Activity.ObjectOption);
        Assert.Equal(_store.Packages.Single().Script, view.Script); // the script, and so its audio, are unchanged
    }

    [Fact]
    public async Task An_older_version_is_409_with_the_latest_and_nothing_changes()
    {
        var response = await SendAsync(HttpMethod.Put, "sections/note", new EditPackageSectionRequest(Guid.NewGuid(), 1, "సరే."));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(2, (await response.Content.ReadFromJsonAsync<PackageView>(Ct))!.ScriptVersion);
        Assert.Null(_store.Packages.Single().TeacherNote);
    }

    [Theory]
    [InlineData(PackageStatus.Generating)]
    [InlineData(PackageStatus.Sent)]
    [InlineData(PackageStatus.Failed)]
    public async Task Nothing_can_change_while_generating_once_sent_or_failed(string status)
    {
        _store.Packages.Single().Status = status;

        Assert.Equal(HttpStatusCode.Conflict,
            (await SendAsync(HttpMethod.Put, "sections/note", new EditPackageSectionRequest(Guid.NewGuid(), 2, "సరే."))).StatusCode);
        Assert.Equal(2, _store.Packages.Single().ScriptVersion);
    }

    [Fact]
    public async Task Replaying_an_edit_is_a_no_op()
    {
        var request = new EditPackageSectionRequest(Guid.NewGuid(), 2, "సరే.");
        await SendAsync(HttpMethod.Put, "sections/note", request);

        var again = await SendAsync(HttpMethod.Put, "sections/note", request);

        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal(3, _store.Packages.Single().ScriptVersion);
    }

    [Theory]
    [InlineData("concept", "", "invalid-output")]
    [InlineData("note", "Ravi did well.", "name")]
    [InlineData("note", "{child} did well.", "placeholder")]
    [InlineData("concept", "ಇಂದು ನಾವು ಕಳೆಯೋಣ.", "script")]
    [InlineData("activity", "3 + 4 = 8 పుల్లలు.", "sum")]
    [InlineData("concept", "ఈ రోజు మనం తీసివేద్దాం.", "activity-number")]
    public async Task A_check_that_fails_saves_nothing_and_says_why(string kind, string text, string problem)
    {
        var response = await SendAsync(HttpMethod.Put, $"sections/{kind}", new EditPackageSectionRequest(Guid.NewGuid(), 2, text));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal(problem, body.GetProperty("errors").GetProperty("problem")[0].GetString());
        Assert.DoesNotContain("Ravi", body.GetRawText());
        Assert.Equal(2, _store.Packages.Single().ScriptVersion);
    }

    [Fact]
    public async Task The_picture_answer_is_never_hers_to_type_and_she_is_told_the_numbers_she_may_use()
    {
        var answer = _problems.Check.Answer.ToString(CultureInfo.InvariantCulture);

        var response = await SendAsync(HttpMethod.Put, "sections/concept",
            new EditPackageSectionRequest(Guid.NewGuid(), 2, $"{A} నుండి {B} తీస్తే {answer} వస్తుంది."));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("errors");
        Assert.Equal("numbers", errors.GetProperty("problem")[0].GetString());
        var allowed = errors.GetProperty("allowed").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains(A, allowed);
        Assert.DoesNotContain(answer, allowed);
    }

    [Fact]
    public async Task The_video_and_her_note_can_be_removed_and_put_back_the_lesson_sections_cannot()
    {
        var package = _store.Packages.Single();
        package.VideoUrl = "https://diksha.gov.in/play/content/do_1";

        var removed = await SendAsync(HttpMethod.Patch, "sections/video", new SetPackageSectionRequest(Guid.NewGuid(), true));
        var view = (await removed.Content.ReadFromJsonAsync<PackageView>(Ct))!;
        Assert.Equal((HttpStatusCode.OK, 3, true), (removed.StatusCode, view.ScriptVersion, view.Video!.Removed));
        Assert.Equal(["video"], view.Removed);

        var back = await SendAsync(HttpMethod.Patch, "sections/video", new SetPackageSectionRequest(Guid.NewGuid(), false));
        Assert.False((await back.Content.ReadFromJsonAsync<PackageView>(Ct))!.Video!.Removed);

        foreach (var kind in new[] { "concept", "activity", "picture" })
        {
            Assert.Equal(HttpStatusCode.BadRequest,
                (await SendAsync(HttpMethod.Patch, $"sections/{kind}", new SetPackageSectionRequest(Guid.NewGuid(), true))).StatusCode);
        }

        // No note yet: nothing to remove.
        Assert.Equal(HttpStatusCode.Conflict,
            (await SendAsync(HttpMethod.Patch, "sections/note", new SetPackageSectionRequest(Guid.NewGuid(), true))).StatusCode);
    }

    [Fact]
    public async Task Another_class_cannot_edit()
    {
        await using var factory = Create();
        using var other = factory.CreateTeacherClient(Guid.NewGuid());

        var response = await other.PutAsJsonAsync(
            $"/api/packages/{_packageId}/sections/note", new EditPackageSectionRequest(Guid.NewGuid(), 2, "సరే."), Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Null(_store.Packages.Single().TeacherNote);
    }

    [Fact]
    public void Her_script_replaces_the_checks_about_the_ai_words_her_note_changes_none()
    {
        var before = new PackageReview(false, 2,
        [
            new VerifierCheck("numbers", true, "ok"), new VerifierCheck("content", false, "too hard"),
            new VerifierCheck("keyWords", false, "said once"), new VerifierCheck("audio", true, "55 seconds"),
        ]);
        var passed = new[] { new VerifierCheck("numbers", true, "hers"), new VerifierCheck("length", true, "40 s") };

        var script = CatchUpEditCheck.Review(before, CatchUpSectionKind.Concept, passed)!;

        Assert.True(script.Passed);
        Assert.Equal(["numbers", "length"], script.Checks.Select(c => c.Name));
        Assert.Same(before, CatchUpEditCheck.Review(before, CatchUpSectionKind.Note, []));
    }

    private async Task<HttpResponseMessage> SendAsync<T>(HttpMethod method, string action, T body)
    {
        await using var factory = Create();
        using var client = factory.CreateTeacherClient(ClassId, TeacherId);
        return await client.SendAsync(
            new HttpRequestMessage(method, $"/api/packages/{_packageId}/{action}") { Content = JsonContent.Create(body) }, Ct);
    }

    private ApiFactory Create() =>
        FakeApi.Create(
            services =>
            {
                services.AddSingleton<ICatchUpStore>(_store);
                services.AddSingleton(new CatchUpQueue());
                services.AddSingleton<TimeProvider>(_clock);
            },
            settings: new Dictionary<string, string?> { ["Demo:Enabled"] = "true" });
}
