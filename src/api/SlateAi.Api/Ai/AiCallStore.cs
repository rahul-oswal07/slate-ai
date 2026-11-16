using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Entities;

namespace SlateAi.Api.Ai;

public interface IAiCallStore
{
    Task AddAsync(AiCall call, CancellationToken cancellationToken);

    Task SetVerdictAsync(Guid id, string verdict, CancellationToken cancellationToken);
}

public sealed class AiCallStore(AppDbContext db) : IAiCallStore
{
    public async Task AddAsync(AiCall call, CancellationToken cancellationToken)
    {
        db.AiCalls.Add(call);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task SetVerdictAsync(Guid id, string verdict, CancellationToken cancellationToken) =>
        db.AiCalls.Where(c => c.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Verdict, AiCallText.Verdict(verdict)), cancellationToken);
}

/// <summary>Fits text to the <c>AiCalls</c> columns, so logging a call can never fail the work it logs (T-137).</summary>
public static class AiCallText
{
    /// <summary>Fits the <c>AiCalls.Error</c> column (300).</summary>
    public static string Error(string text) => text.Length <= 300 ? text : text[..300];

    /// <summary>Fits the <c>AiCalls.Verdict</c> column (100): A4's "fail:" with several reasons ran past it (T-137).</summary>
    public static string Verdict(string text) => text.Length <= 100 ? text : text[..100];
}
