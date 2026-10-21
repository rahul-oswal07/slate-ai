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
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Verdict, verdict), cancellationToken);
}

internal static class AiCallText
{
    /// <summary>Fits the <c>AiCalls.Error</c> column (300).</summary>
    public static string Error(string text) => text.Length <= 300 ? text : text[..300];
}
