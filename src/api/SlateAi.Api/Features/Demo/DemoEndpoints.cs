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
using SlateAi.Api.Features.Delivery;
using SlateAi.Api.Features.EveningMessages;

namespace SlateAi.Api.Features.Demo;

public sealed record SatsFeedRequest(Guid ClassId, DateOnly Date, string Mode);

public sealed record DemoResetRequest(Guid ClientRequestId);

/// <param name="ResetAt">The "Under the hood" panel lists AI calls from here.</param>
/// <param name="PreMade">Whether a pre-made Ready catch-up was restored.</param>
public sealed record DemoResetResponse(DateTimeOffset ResetAt, bool PreMade);

public sealed record DemoSendNowRequest(Guid ClientRequestId);

/// <summary>What "Send messages now" sent.</summary>
/// <param name="EveningBlocked">Why tonight's message didn't go (an <see cref="EveningBlock"/> code), or null.</param>
/// <param name="EveningSent">Families sent tonight's message by this tap.</param>
/// <param name="CatchUpsSent">Approved catch-ups sent by this tap.</param>
public sealed record DemoSendNowResponse(string? EveningBlocked, int EveningSent, int CatchUpsSent);

/// <summary>Who the demo shell is about.</summary>
/// <param name="ParentChildId">The child whose family the demo parent phone shows: Manjunath, whose family reads Kannada (T-93, D-94).</param>
/// <param name="LastResetAt">When anyone last reset the demo, so every viewer's "Under the hood" starts there; null if never.</param>
/// <param name="ParentChildName">The child's first name (fictional), so the family phone is labelled with the family.</param>
/// <param name="ParentLanguage">The family's home language code, e.g. "kn".</param>
public sealed record DemoInfoResponse(
    Guid ClassId, Guid ParentChildId, DateTimeOffset? LastResetAt, string ParentChildName, string ParentLanguage);

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

    public static IServiceCollection AddDemo(this IServiceCollection services) =>
        services.AddScoped<IDemoStore, DemoStore>().AddScoped<DemoSendOnApprove>();

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
            .WithDescription(
                "404 unless signed in as the demo teacher. Demo only.");

        app.MapPost("/api/demo/reset", ResetAsync)
            .RequireAuthorization()
            .WithTags("Demo")
            .WithSummary("Put the demo class back to the start of the story")
            .WithDescription(
                "Deletes everything the class has done and re-adds the 36 seeded children, then writes the five school " +
                "days before today (SATS registers and confirmed lessons; only Manjunath (Kannada) away, on the last two) " +
                "and today's simulated register, stamped 12:00 IST, with Manjunath away and no lesson yet (T-93). " +
                "When data/seed/demo-catchup.json exists in Manjunath's language, that catch-up is restored as Ready. " +
                "The evening template and the teacher's sign-in stay as they are. " +
                "A replayed clientRequestId changes nothing. 404 unless signed in as the demo teacher. Demo only.");

        app.MapPost("/api/demo/send-now", SendNowAsync)
            .RequireAuthorization()
            .WithTags("Demo")
            .WithSummary("Send tonight's messages and approved catch-ups now, without waiting for 6 pm")
            .WithDescription(
                "Does for the demo class what the 6 pm job does: tonight's evening message from the approved template, " +
                "then every approved, unsent catch-up of the week. Same rules as the job: nothing unapproved, only " +
                "families with consent Yes who haven't stopped (guardrail 3). Then delivers what is queued to registered " +
                "WhatsApp test phones when `Delivery:Channel` is whatsapp (T-40). Safe to repeat: nobody gets a message " +
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
                "mode=arrive releases it and stores the simulated register with receivedAt = now, or the feed's usual " +
                "time (12:00 IST) when that is later, so a morning demo doesn't show 2:37 AM (T-93). Demo only.");

        app.MapGet("/api/demo/voice-note", GetVoiceNoteAsync)
            .RequireAuthorization()
            .WithTags("Demo")
            .WithSummary("The sample Kannada voice note about Nalikali p 4")
            .WithDescription(
                "The clip behind Today's \"Use the sample voice note\" (T-93): the teacher app uploads it like a recording, " +
                "so it goes through the real STT, name scrubber and A1. 404 when Demo:VoiceNoteFile is missing or the " +
                "signed-in teacher isn't the demo teacher. Demo only.");
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
            var usual = SchoolClock.At(request.Date, options.Value.ArrivesAt).ToUniversalTime();
            var register = new SourceResult.Arrived(source.Simulate(request.ClassId, request.Date, roster), now > usual ? now : usual);
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
        CancellationToken cancellationToken)
    {
        var seed = await DemoSeedFile.LoadAsync(database.Value.SeedFile, cancellationToken);
        if (http.User.GetClassId() != seed.Class.Id)
        {
            return TypedResults.NotFound();
        }

        var lastReset = await store.GetLastResetAsync(cancellationToken);
        var child = seed.Children.Single(c => c.RollNo == DemoStory.ManjunathRollNo);
        return TypedResults.Ok(new DemoInfoResponse(seed.Class.Id, child.Id, lastReset, child.FirstName, child.HomeLanguage));
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
        var days = DemoStory.Build(today, roster, source.IsSchoolDay);
        var snapshot = await DemoSnapshot.LoadAsync(demo.Value.SnapshotFile, cancellationToken);

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
                DemoStory.StarWeeks(today, roster, source.IsSchoolDay),
                new DemoRegisterDay(today, source.Simulate(classId, today, roster))),
            now,
            cancellationToken);
        holds.ReleaseAll(classId);

        return TypedResults.Ok(new DemoResetResponse(now, owner is not null));
    }

    private static async Task<Results<Ok<DemoSendNowResponse>, NotFound, ValidationProblem>> SendNowAsync(
        DemoSendNowRequest request,
        HttpContext http,
        EveningSender evening,
        CatchUpSender catchUps,
        DeliveryDispatcher delivery,
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
        // Registered test phones get WhatsApp now rather than on the next minute's tick (T-40); demo channel: nothing.
        await delivery.RunOnceAsync(cancellationToken);
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

    private static async Task<Results<PhysicalFileHttpResult, NotFound>> GetVoiceNoteAsync(
        HttpContext http, IOptions<DatabaseOptions> database, IOptions<DemoOptions> demo, CancellationToken cancellationToken)
    {
        var seed = await DemoSeedFile.LoadAsync(database.Value.SeedFile, cancellationToken);
        return http.User.GetClassId() == seed.Class.Id && DemoVoiceNote.Path(demo.Value) is { } path
            ? TypedResults.PhysicalFile(path, "audio/webm")
            : TypedResults.NotFound();
    }
}
