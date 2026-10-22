using System.Text.Json;
using SlateAi.Api.Ai;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.Ledger;

namespace SlateAi.Api.Features.CatchUps.Compression;

/// <summary>
/// A2, prerequisite-aware compression (FR-06): picks at most 2 of a child's missed lessons that next week builds
/// on, with a reason for each kept and dropped lesson. Claude chooses only from the code-computed candidate set;
/// anything else falls back to the nearest-prerequisite rule (spec §5.1, guardrail 2).
/// </summary>
public sealed class Compressor(
    ILlmClient llm,
    IAiCallStore aiCalls,
    PromptLibrary prompts,
    CurriculumGraph curriculum,
    ILedgerStore ledger)
{
    public const string PromptId = "a2-compression";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>One result per child who missed a lesson in the school week up to <paramref name="date"/>.</summary>
    /// <param name="skip">Children who already have this week's catch-up: no LLM call for them.</param>
    public async Task<IReadOnlyList<A2Result>> CompressClassAsync(
        Guid classId, DateOnly date, CancellationToken cancellationToken, IReadOnlySet<Guid>? skip = null)
    {
        if (await ledger.LoadAsync(classId, cancellationToken) is not { } data)
        {
            return [];
        }

        var inputs = CompressionPlanner.Plan(LedgerBuilder.Build(classId, data), data.Lessons, date, curriculum);

        // Children who missed the same lessons on the same days get the same request: ask Claude once per request.
        var answers = new Dictionary<string, A2Result>(StringComparer.Ordinal);
        var results = new List<A2Result>();
        foreach (var input in inputs.Where(i => skip?.Contains(i.ChildId) != true))
        {
            var key = UserContent(input);
            if (!answers.TryGetValue(key, out var answer))
            {
                answer = await CompressAsync(input, cancellationToken);
                answers[key] = answer;
            }

            results.Add(answer with { ChildId = input.ChildId });
        }

        return results;
    }

    /// <summary>A2 for one child. Also what the eval suite runs.</summary>
    public async Task<A2Result> CompressAsync(CompressionInput input, CancellationToken cancellationToken)
    {
        if (input.Candidates.Count == 0)
        {
            // Nothing missed is a prerequisite of next week: no choice to make, so no LLM call.
            return A2Validator.Rule(input, "rule:no-candidates");
        }

        // The default 4096: with five missed lessons and a reason for each, 1024 was truncated in the eval.
        var request = new LlmRequest(prompts.Get(PromptId), UserContent(input), A2Validator.Schema(input));
        LlmResponse response;
        try
        {
            response = await llm.CompleteJsonAsync(request, cancellationToken);
        }
        catch (LlmException e)
        {
            // The AiCall row already records the failure.
            return A2Validator.Rule(input, $"error:{e.Failure}");
        }

        var result = A2Validator.Validate(response.Json, input);
        if (response.AiCallId != Guid.Empty)
        {
            await aiCalls.SetVerdictAsync(response.AiCallId, result.Verdict, cancellationToken);
        }

        return result;
    }

    /// <summary>IDs, curriculum data and dates only: no names or child IDs reach Claude (guardrail 1).</summary>
    internal string UserContent(CompressionInput input) =>
        JsonSerializer.Serialize(new
        {
            missed = input.Missed.Select(m => curriculum.Get(m.Id) is { } c
                ? new { c.Id, c.NameEn, c.NameKn, c.Chapter, c.Prerequisites, Dates = m.Dates }
                : null).OfType<object>(),
            candidates = input.Candidates,
            nextWeek = input.NextWeek.Select(id => new { Id = id, curriculum.Get(id)?.NameEn, curriculum.Get(id)?.Prerequisites }),
            priorChecks = Array.Empty<object>(),
        }, Json);
}
