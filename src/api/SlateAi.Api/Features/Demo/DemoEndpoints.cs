using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Seed;
using SlateAi.Api.Features.Attendance;
using SlateAi.Api.Features.Attendance.Source;
using SlateAi.Api.Features.Auth;
using SlateAi.Api.Features.CatchUps;
using SlateAi.Api.Features.CatchUps.Compression;
using SlateAi.Api.Features.EveningMessages;

namespace SlateAi.Api.Features.Demo;

public sealed record SatsFeedRequest(Guid ClassId, DateOnly Date, string Mode);

public sealed record DemoResetRequest(Guid ClientRequestId);

/// <param name="ResetAt">The "Under the hood" panel lists AI calls from here.</param>
/// <param name="PreMade">Whether a pre-made Ready catch-up was restored.</param>
/// <param name="EveningSent">Video story: evening messages sent for the week's earlier days (0 if the template isn't approved).</param>
public sealed record DemoResetResponse(DateTimeOffset ResetAt, bool PreMade, int EveningSent = 0);

public sealed record DemoSendNowRequest(Guid ClientRequestId);

/// <summary>What "Send messages now" sent.</summary>
/// <param name="EveningBlocked">Why tonight's message didn't go (an <see cref="EveningBlock"/> code), or null.</param>
/// <param name="EveningSent">Families sent tonight's message by this tap.</param>
/// <param name="CatchUpsSent">Approved catch-ups sent by this tap.</param>
public sealed record DemoSendNowResponse(string? EveningBlocked, int EveningSent, int CatchUpsSent);

/// <summary>Who the demo shell is about.</summary>
/// <param name="ParentChildId">The child whose family the demo parent phone shows: Rohan, whose family reads Hindi (the EN toggle shows the app in English); Ananya (Kannada) in the video story.</param>
/// <param name="LastResetAt">When anyone last reset the demo, so every viewer's "Under the hood" starts there; null if never.</param>
/// <param name="Story">"judges" (the guide on /demo) or "video" (T-70, D-85).</param>
public sealed record DemoInfoResponse(Guid ClassId, Guid ParentChildId, DateTimeOffset? LastResetAt, string Story);

/// <summary>
/// Demo-shell controls (FR-14, D-69). Mapped only when <c>Demo:Enabled</c> is true (the default, for judges);
/// a real school deployment turns it off and the endpoints don't exist.
/// </summary>
public static class DemoEndpoints
{
    public const string Waiting = "waiting";
    public const string Arrive = "arrive";

    /// <summary>How many AI calls the "Under the hood" panel shows.</summary>
    public const int AiCallLimit = 50;

    /// <summary>The feed's scripted absences in the video story: Ananya away every day (T-70).</summary>
    public const string VideoScenarioFile = "data/seed/sats-scenario-video.json";

    public static IServiceCollection AddDemo(this IServiceCollection services)
    {
        // One setting switches the whole video story, today's register included.
        services.AddOptions<SatsOptions>().PostConfigure<IOptions<DemoOptions>>((sats, demo) =>
        {
            if (demo.Value.IsVideo)
            {
                sats.ScenarioFile = VideoScenarioFile;
            }
        });
        return services.AddScoped<IDemoStore, DemoStore>();
    }

    public static IEndpointRouteBuilder MapDemoEndpoints(this IEndpointRouteBuilder app)
    {
        if (!app.ServiceProvider.GetRequiredService<IOptions<DemoOptions>>().Value.Enabled)
        {
            return app;
        }

        app.MapPost("/api/demo/login", LoginAsync)
            .WithTags("Demo")
            .WithSummary("Sign in as the demo teacher with one tap")
            .WithDescription(
                "Starts the same 30-day session as the PIN sign-in, for the seeded demo teacher only, so judges need " +
                "no PIN (FR-14). 404 when the demo class isn't seeded. Demo only.");

        app.MapGet("/api/demo", GetInfoAsync)
            .RequireAuthorization()
            .WithTags("Demo")
            .WithSummary("The demo class, whose family the parent phone shows, and the last reset")
            .WithDescription("404 unless signed in as the demo teacher. Demo only.");

        app.MapPost("/api/demo/reset", ResetAsync)
            .RequireAuthorization()
            .WithTags("Demo")
            .WithSummary("Put the demo class back to the start of the story")
            .WithDescription(
                "Deletes everything the class has done and re-adds the 36 seeded children, then writes the five school " +
                "days before today (SATS registers and confirmed lessons; only Ravi (Telugu, this week) and Manjunath (Kannada, " +
                "2 days, back today) away; Rohan (Hindi) is away today, so the week makes three catch-ups) and, " +
                "when data/seed/demo-catchup.json exists, that catch-up as Ready (not approved) for the story child of its " +
                "language. Today's register " +
                "comes from the simulated feed as usual. The evening template and the teacher's sign-in stay as they are. " +
                "A replayed clientRequestId changes nothing. 404 unless signed in as the demo teacher. Demo only.");

        app.MapPost("/api/demo/send-now", SendNowAsync)
            .RequireAuthorization()
            .WithTags("Demo")
            .WithSummary("Send tonight's messages and approved catch-ups now, without waiting for 6 pm")
            .WithDescription(
                "Does for the demo class what the 6 pm job does: tonight's evening message from the approved template, " +
                "then every approved, unsent catch-up of the week. Same rules as the job: nothing unapproved, only " +
                "families with consent Yes who haven't stopped (guardrail 3). Safe to repeat: nobody gets a message " +
                "twice. 404 unless signed in as the demo teacher. Demo only.");

        app.MapGet("/api/demo/family/{childId:guid}", GetFamilyAsync)
            .RequireAuthorization()
            .WithTags("Demo")
            .WithSummary("The simulated parent phone: links sent to a family")
            .WithDescription(
                "Evening and catch-up links sent to the child's family, newest first. The phone opens each one at " +
                "/p/{token} like a real parent. 404 for a child outside the signed-in teacher's class. Demo only.");

        app.MapGet("/api/demo/ai-calls", GetAiCallsAsync)
            .RequireAuthorization()
            .WithTags("Demo")
            .WithSummary("\"Under the hood\": the latest Claude and Sarvam calls")
            .WithDescription(
                $"Up to {AiCallLimit} calls, newest first, from `since` when given: purpose (prompt ID or stt/tts), model, " +
                "prompt version, tokens, latency, the verdict of validating the output, and who the call was about: first " +
                "names of demo-class children (looked up from the demo roster; the AI log stores IDs only, T-73), and a " +
                "count of children in other classes, never named. No prompt text or transcripts are stored. Demo only.");

        if (app.ServiceProvider.GetRequiredService<IHostEnvironment>().IsDevelopment())
        {
            app.MapGet("/api/demo/snapshot/{packageId:guid}", GetSnapshotAsync)
                .RequireAuthorization()
                .WithTags("Demo")
                .WithSummary("Export a catch-up as the demo's pre-made one")
                .WithDescription(
                    "Development only. Save the body as data/seed/demo-catchup.json; the Reset restores it as Ready. " +
                    "404 for another class's package.");
        }

        app.MapPost("/api/demo/sats-feed", SatsFeedAsync)
            .RequireAuthorization()
            .WithTags("Demo")
            .WithSummary("Make the simulated SATS register late, or arrive now")
            .WithDescription(
                "mode=waiting removes the date's register and holds SATS back (Today shows \"not in yet\"); " +
                "mode=arrive releases it and stores the simulated register with receivedAt = now. Demo only.");
        return app;
    }

    private static async Task<Results<Ok<AttendanceDayResponse>, NotFound, ValidationProblem>> SatsFeedAsync(
        SatsFeedRequest request,
        HttpContext http,
        IAttendanceStore store,
        SimulatedSatsSource source,
        SatsDemoHolds holds,
        IOptions<SatsOptions> options,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        if (request.Mode is not (Waiting or Arrive))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["mode"] = [$"Use \"{Waiting}\" or \"{Arrive}\"."],
            });
        }

        if (http.User.GetClassId() != request.ClassId
            || await store.GetRosterAsync(request.ClassId, cancellationToken) is not { } roster)
        {
            return TypedResults.NotFound();
        }

        var now = clock.GetUtcNow();
        IReadOnlyList<AttendanceRow> rows = [];
        if (request.Mode == Waiting)
        {
            holds.Hold(request.ClassId, request.Date);
            await store.RemoveDayAsync(request.ClassId, request.Date, cancellationToken);
        }
        else
        {
            holds.Release(request.ClassId, request.Date);
            var register = new SourceResult.Arrived(source.Simulate(request.ClassId, request.Date, roster), now);
            rows = await store.SaveSourceRegisterAsync(
                request.ClassId, request.Date, roster.Select(c => c.Id).ToList(), register, cancellationToken);
        }

        return TypedResults.Ok(AttendanceDay.Describe(request.ClassId, request.Date, roster, rows, false, now, options.Value));
    }

    private static async Task<Results<Ok<MeResponse>, NotFound>> LoginAsync(
        HttpContext http, IAuthStore auth, IOptions<DatabaseOptions> database, CancellationToken cancellationToken)
    {
        var seed = await DemoSeedFile.LoadAsync(database.Value.SeedFile, cancellationToken);
        if (await auth.GetMeAsync(seed.Teacher.Id, cancellationToken) is not { } me)
        {
            return TypedResults.NotFound();
        }

        await http.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            AuthClaims.Teacher(me.TeacherId, me.ClassId, CookieAuthenticationDefaults.AuthenticationScheme),
            new AuthenticationProperties { IsPersistent = true });
        return TypedResults.Ok(me);
    }

    private static async Task<Results<Ok<DemoInfoResponse>, NotFound>> GetInfoAsync(
        HttpContext http,
        IDemoStore store,
        IOptions<DatabaseOptions> database,
        IOptions<DemoOptions> demo,
        CancellationToken cancellationToken)
    {
        var seed = await DemoSeedFile.LoadAsync(database.Value.SeedFile, cancellationToken);
        if (http.User.GetClassId() != seed.Class.Id)
        {
            return TypedResults.NotFound();
        }

        var lastReset = await store.GetLastResetAsync(cancellationToken);
        var video = demo.Value.IsVideo;
        return TypedResults.Ok(new DemoInfoResponse(
            seed.Class.Id,
            Child(seed, DemoStory.ParentRollNo(video)),
            lastReset,
            video ? DemoOptions.VideoStory : DemoOptions.JudgesStory));
    }

    private static async Task<Results<Ok<DemoResetResponse>, NotFound>> ResetAsync(
        DemoResetRequest request,
        HttpContext http,
        IDemoStore store,
        SimulatedSatsSource source,
        SatsDemoHolds holds,
        IOptions<SatsOptions> sats,
        IOptions<DatabaseOptions> database,
        IOptions<DemoOptions> demo,
        IOptions<EveningOptions> eveningOptions,
        EveningSender evening,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var seed = await DemoSeedFile.LoadAsync(database.Value.SeedFile, cancellationToken);
        var classId = seed.Class.Id;
        if (http.User.GetClassId() != classId)
        {
            return TypedResults.NotFound();
        }

        var now = clock.GetUtcNow();
        var today = SchoolClock.Today(clock);
        var weekOf = CompressionPlanner.Week(today).From;
        var roster = seed.Children.Select(c => new RosterChild(c.Id, c.RollNo, c.FirstName)).ToList();
        var video = demo.Value.IsVideo;
        var days = video ? DemoStory.Video(today, roster) : DemoStory.Build(today, roster, source.IsSchoolDay);
        // The video story makes every catch-up live, on camera.
        var snapshot = video ? null : await DemoSnapshot.LoadAsync(demo.Value.SnapshotFile, cancellationToken);

        // The pre-made catch-up goes to the story child who speaks its language, with what they missed so far.
        var owner = snapshot is null
            ? null
            : seed.Children.FirstOrDefault(c => DemoStory.StoryRollNos.Contains(c.RollNo) && c.HomeLanguage == snapshot.Language);
        var missed = owner is null
            ? []
            : days.Where(d => d.Date >= weekOf && d.AbsentChildIds.Contains(owner.Id)).Select(d => d.CompetencyId).ToList();

        var result = await store.ResetAsync(
            new DemoReset(
                request.ClientRequestId, seed, days, sats.Value.ArrivesAt, weekOf, owner is null ? null : snapshot, owner?.Id, missed,
                DemoStory.StarWeeks(today, roster, source.IsSchoolDay)),
            now,
            cancellationToken);
        holds.ReleaseAll(classId);

        // Video story: each earlier evening's message has gone, at that day's 6 pm, through the 6 pm job's own path
        // (approved template, consent Yes, guardrail 3). Nothing goes if the template isn't approved.
        var eveningSent = 0;
        if (video && result == DemoResetResult.Reset)
        {
            foreach (var day in days)
            {
                var sentAt = SchoolClock.At(day.Date, eveningOptions.Value.SendAt).ToUniversalTime();
                eveningSent += (await evening.SendAsync(classId, day.Date, cancellationToken, sentAt))?.Sent ?? 0;
            }
        }

        return TypedResults.Ok(new DemoResetResponse(now, owner is not null, eveningSent));
    }

    private static async Task<Results<Ok<DemoSendNowResponse>, NotFound, ValidationProblem>> SendNowAsync(
        DemoSendNowRequest request,
        HttpContext http,
        EveningSender evening,
        CatchUpSender catchUps,
        IOptions<DatabaseOptions> database,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        if (request.ClientRequestId == Guid.Empty)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["clientRequestId"] = ["Required."],
            });
        }

        var seed = await DemoSeedFile.LoadAsync(database.Value.SeedFile, cancellationToken);
        var classId = seed.Class.Id;
        if (http.User.GetClassId() != classId)
        {
            return TypedResults.NotFound();
        }

        // No ProcessedRequest needed: both senders send each family at most once, so a replay sends nothing new.
        var today = SchoolClock.Today(clock);
        var before = (await evening.PlanAsync(classId, today, cancellationToken))?.Sent ?? 0;
        var day = await evening.SendAsync(classId, today, cancellationToken);
        var sent = await catchUps.SendApprovedAsync(classId, today, cancellationToken);
        return TypedResults.Ok(new DemoSendNowResponse(day?.Blocked, (day?.Sent ?? 0) - before, sent));
    }

    private static async Task<Results<Ok<IReadOnlyList<FamilyMessage>>, NotFound>> GetFamilyAsync(
        Guid childId, HttpContext http, IDemoStore store, CancellationToken cancellationToken) =>
        http.User.GetClassId() is { } classId
        && await store.GetFamilyAsync(classId, childId, cancellationToken) is { } messages
            ? TypedResults.Ok(messages)
            : TypedResults.NotFound();

    private static async Task<Ok<IReadOnlyList<AiCallRow>>> GetAiCallsAsync(
        DateTimeOffset? since, IDemoStore store, IOptions<DatabaseOptions> database, CancellationToken cancellationToken)
    {
        // Only the demo class's fictional children are named; anyone else (a real class on staging) stays a count.
        var seed = await DemoSeedFile.LoadAsync(database.Value.SeedFile, cancellationToken);
        var names = seed.Children.ToDictionary(c => c.Id, c => c.FirstName);
        var calls = await store.GetAiCallsAsync(since, AiCallLimit, cancellationToken);
        return TypedResults.Ok<IReadOnlyList<AiCallRow>>(calls
            .Select(c => c with
            {
                ChildIds = null,
                Children = (c.ChildIds ?? []).Where(names.ContainsKey).Select(id => names[id]).ToList(),
                OtherChildren = (c.ChildIds ?? []).Count(id => !names.ContainsKey(id)),
            })
            .ToList());
    }

    private static async Task<Results<Ok<DemoSnapshot>, NotFound>> GetSnapshotAsync(
        Guid packageId, HttpContext http, IDemoStore store, CancellationToken cancellationToken) =>
        http.User.GetClassId() is { } classId
        && await store.ExportAsync(classId, packageId, cancellationToken) is { } snapshot
            ? TypedResults.Ok(snapshot)
            : TypedResults.NotFound();

    private static Guid Child(DemoSeedFile seed, int rollNo) => seed.Children.Single(c => c.RollNo == rollNo).Id;
}
