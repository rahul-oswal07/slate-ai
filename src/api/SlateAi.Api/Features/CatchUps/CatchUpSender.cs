using System.Buffers.Text;
using System.Security.Cryptography;
using SlateAi.Api.Features.CatchUps.Compression;

namespace SlateAi.Api.Features.CatchUps;

/// <summary>The send rules for catch-ups (guardrail 3, FR-08, D-19, D-39). Pure.</summary>
public static class CatchUpSendRules
{
    /// <summary>Only an approved package, at the exact version she approved.</summary>
    public static bool CanSend(string status, int? approvedScriptVersion, int scriptVersion) =>
        status == PackageStatus.Approved && approvedScriptVersion == scriptVersion;

    /// <summary>The family gets the link only with consent "yes" and not stopped; others stay on the buddy card.</summary>
    public static bool ReachesFamily(string consent, bool stopped) => consent == "yes" && !stopped;
}

/// <summary>
/// The only way a catch-up leaves the school (guardrail 3): approved packages get a link token per consenting
/// family. Called by the 6 pm job, or by the send endpoint (any time in demo mode).
/// </summary>
public sealed class CatchUpSender(ICatchUpStore store, TimeProvider clock, ILogger<CatchUpSender> logger)
{
    /// <summary>Spec §4.4: parent links expire after 14 days.</summary>
    public static readonly TimeSpan LinkLife = TimeSpan.FromDays(14);

    public async Task<PackageChange> SendAsync(Guid packageId, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var result = await store.SendAsync(packageId, NewToken, now, now + LinkLife, cancellationToken);
        if (result == PackageChange.Applied)
        {
            logger.LogInformation("Catch-up {PackageId} sent", packageId);
        }

        return result;
    }

    /// <summary>Sends every approved, unsent catch-up of the class for the week of <paramref name="date"/> (the 6 pm job).</summary>
    public async Task<int> SendApprovedAsync(Guid classId, DateOnly date, CancellationToken cancellationToken)
    {
        var sent = 0;
        foreach (var id in await store.GetApprovedUnsentAsync(classId, CompressionPlanner.Week(date).From, cancellationToken))
        {
            if (await SendAsync(id, cancellationToken) == PackageChange.Applied)
            {
                sent++;
            }
        }

        return sent;
    }

    /// <summary>128 random bits, URL-safe: the only key to the parent page (T-15).</summary>
    private static string NewToken() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(16));
}
