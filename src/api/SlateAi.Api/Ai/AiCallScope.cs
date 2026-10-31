namespace SlateAi.Api.Ai;

/// <summary>
/// Which children the AI calls made inside it are about (T-73, D-87), so the "Under the hood" panel can say who a call
/// was for. Only IDs: names are never sent to an AI service (guardrail 1) nor stored in the AI log; the panel looks
/// them up. Flows through <c>await</c>, so the logged clients see it without any change to their signatures.
/// </summary>
public static class AiCallScope
{
    private static readonly AsyncLocal<IReadOnlyList<Guid>?> Children = new();

    /// <summary>The children of the innermost scope, or null outside one (a class-level call).</summary>
    public static IReadOnlyList<Guid>? Current => Children.Value;

    /// <summary>Tags the calls made until the returned scope is disposed.</summary>
    public static IDisposable For(IReadOnlyList<Guid> childIds)
    {
        var outer = Children.Value;
        Children.Value = childIds.Count > 0 ? childIds : null;
        return new Restore(outer);
    }

    private sealed class Restore(IReadOnlyList<Guid>? outer) : IDisposable
    {
        public void Dispose() => Children.Value = outer;
    }
}
