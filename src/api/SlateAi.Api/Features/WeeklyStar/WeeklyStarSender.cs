using System.Buffers.Text;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using SlateAi.Api.Features.CatchUps.Compression;
using SlateAi.Api.Features.EveningMessages;
using SlateAi.Api.Features.Ledger;
using SlateAi.Api.Features.Stars;

namespace SlateAi.Api.Features.WeeklyStar;

/// <summary>Why no star message goes for the week.</summary>
public static class WeeklyStarBlock
{
    /// <summary>She hasn't approved the current wording (guardrail 3).</summary>
    public const string NotApproved = "notApproved";

    /// <summary>The week has no register at all.</summary>
    public const string NoRegister = "noRegister";

    /// <summary>The week isn't over: Saturday's register isn't in yet.</summary>
    public const string NotFinished = "notFinished";
}

/// <param name="Blocked">A <see cref="WeeklyStarBlock"/> value, or null.</param>
/// <param name="Earned">Children present every school day of the week.</param>
/// <param name="Sent">Star messages added by this run (0 when they all went already).</param>
public sealed record WeeklyStarRun(DateOnly WeekOf, string? Blocked, int Earned, int Sent);

/// <summary>
/// Sends the weekly star (T-125, D-118) to the families of the children present every school day of a finished week.
/// Code only, no LLM: <see cref="StarRules"/> decides who earned it, the approved wording is filled with {child}, and only
/// families with consent Yes who haven't stopped get it (D-39); WhatsApp then goes only to a saved number (D-114).
/// Once per child per week.
/// </summary>
public sealed class WeeklyStarSender(
    ILedgerStore ledger,
    IWeeklyStarStore store,
    WeeklyStarWording wording,
    IOptions<WeeklyStarOptions> options,
    TimeProvider clock,
    ILogger<WeeklyStarSender> logger)
{
    /// <summary>
    /// The 6 pm job: on Saturday once its register is in, and again on Sunday for a week whose Saturday had no school
    /// (or whose register came late). Other days do nothing (null).
    /// </summary>
    public Task<WeeklyStarRun?> SendDueAsync(Guid classId, DateOnly today, CancellationToken cancellationToken) =>
        today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
            ? SendWeekAsync(classId, CompressionPlanner.Week(today).From, today, cancellationToken)
            : Task.FromResult<WeeklyStarRun?>(null);

    /// <summary>"Send messages now" on /demo: the latest finished week, whatever the day.</summary>
    public async Task<WeeklyStarRun?> SendLatestAsync(Guid classId, DateOnly today, CancellationToken cancellationToken)
    {
        var thisWeek = CompressionPlanner.Week(today).From;
        if (!await ApprovedAsync(classId, cancellationToken))
        {
            return new WeeklyStarRun(thisWeek.AddDays(-7), WeeklyStarBlock.NotApproved, 0, 0);
        }

        if (await ledger.LoadAsync(classId, cancellationToken) is not { } data)
        {
            return null;
        }

        var weekOf = StarRules.For(data, thisWeek, today)?.Finished == true ? thisWeek : thisWeek.AddDays(-7);
        return await SendWeekAsync(classId, weekOf, today, data, cancellationToken);
    }

    private async Task<WeeklyStarRun?> SendWeekAsync(Guid classId, DateOnly weekOf, DateOnly today, CancellationToken cancellationToken)
    {
        // Approval first: without it nothing is loaded or sent (guardrail 3).
        if (!await ApprovedAsync(classId, cancellationToken))
        {
            return new WeeklyStarRun(weekOf, WeeklyStarBlock.NotApproved, 0, 0);
        }

        return await ledger.LoadAsync(classId, cancellationToken) is { } data
            ? await SendWeekAsync(classId, weekOf, today, data, cancellationToken)
            : null;
    }

    private async Task<WeeklyStarRun> SendWeekAsync(
        Guid classId, DateOnly weekOf, DateOnly today, LedgerData data, CancellationToken cancellationToken)
    {
        var stars = StarRules.For(data, weekOf, today);
        var blocked = stars is null ? WeeklyStarBlock.NoRegister
            : !stars.Finished ? WeeklyStarBlock.NotFinished
            : null;
        var earned = stars?.Children.Count ?? 0;
        if (blocked is not null || earned == 0)
        {
            return new WeeklyStarRun(weekOf, blocked, earned, 0);
        }

        var sent = await store.GetSentChildIdsAsync(classId, weekOf, cancellationToken);
        var families = data.Children.ToDictionary(c => c.Id);
        var now = clock.GetUtcNow();
        var pending = new List<NewWeeklyStar>();
        foreach (var child in stars!.Children)
        {
            var family = families[child.ChildId];
            var text = wording.Languages.GetValueOrDefault(family.HomeLanguage);
            var outcome = EveningEligibility.Decide(family.Consent, family.StoppedAt is not null, sent.Contains(child.ChildId), text is not null);
            if (outcome == EveningOutcome.WillSend)
            {
                pending.Add(new NewWeeklyStar(
                    child.ChildId, family.HomeLanguage, wording.Version, text!.Message, text.Badge, NewToken(), now,
                    now.AddDays(options.Value.LinkDays)));
            }
        }

        var added = pending.Count == 0 ? 0 : await store.AddAsync(classId, weekOf, pending, cancellationToken);
        if (added > 0)
        {
            // IDs and counts only (NFR-05).
            logger.LogInformation(
                "Weekly star for class {ClassId}, week of {WeekOf}: {Earned} earned, {Added} sent", classId, weekOf, earned, added);
        }

        return new WeeklyStarRun(weekOf, null, earned, added);
    }

    private async Task<bool> ApprovedAsync(Guid classId, CancellationToken cancellationToken) =>
        await store.GetApprovedVersionAsync(classId, cancellationToken) == wording.Version;

    /// <summary>128 random bits, URL-safe: the only key to the page.</summary>
    private static string NewToken() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(16));
}
