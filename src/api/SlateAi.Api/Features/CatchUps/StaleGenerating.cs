namespace SlateAi.Api.Features.CatchUps;

/// <summary>
/// A catch-up cut off while its words were being written (T-136). The writing queue is in memory, so a restart or a
/// deploy leaves the package "generating" for good. After <see cref="After"/> it reads as failed with
/// <c>error:stale</c>, and Try again takes it like any failed one. Pure.
/// </summary>
public static class StaleGenerating
{
    public static readonly TimeSpan After = TimeSpan.FromMinutes(5);

    public const string Verdict = "error:stale";

    /// <summary>Still "generating" long after it started: nothing is writing it any more.</summary>
    public static bool IsStale(string status, DateTimeOffset updatedAt, DateTimeOffset now) =>
        status == PackageStatus.Generating && now - updatedAt > After;

    /// <summary>Try again (Simpler, Shorter): from ready, needs review, failed, or stale.</summary>
    public static bool CanRegenerate(string status, DateTimeOffset updatedAt, DateTimeOffset now) =>
        status is PackageStatus.Ready or PackageStatus.NeedsReview or PackageStatus.Failed || IsStale(status, updatedAt, now);

    /// <summary>How the teacher sees it: a stale package as failed.</summary>
    public static PackageView Shown(PackageView view, DateTimeOffset now) =>
        IsStale(view.Status, view.UpdatedAt, now) ? view with { Status = PackageStatus.Failed, Verdict = view.Verdict ?? Verdict } : view;
}
