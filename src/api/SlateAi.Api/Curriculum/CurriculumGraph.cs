namespace SlateAi.Api.Curriculum;

/// <summary>
/// Pure, immutable prerequisite graph. Assumes validated input (see <see cref="CurriculumLoader"/>): unique IDs,
/// known prerequisites, no cycles.
/// </summary>
public sealed class CurriculumGraph
{
    private readonly Dictionary<string, Competency> _byId;

    public CurriculumGraph(IEnumerable<Competency> competencies)
    {
        _byId = competencies.ToDictionary(c => c.Id, StringComparer.Ordinal);
        All = _byId.Values.OrderBy(c => c.Sequence).ThenBy(c => c.Id, StringComparer.Ordinal).ToList();
    }

    public static CurriculumGraph Empty { get; } = new([]);

    /// <summary>Every competency in teaching order.</summary>
    public IReadOnlyList<Competency> All { get; }

    public bool Contains(string id) => _byId.ContainsKey(id);

    public Competency? Get(string id) => _byId.GetValueOrDefault(id);

    /// <summary>A chapter's competencies in teaching order (A1 input, FR-04 picker).</summary>
    public IReadOnlyList<Competency> InChapter(int chapter) => All.Where(c => c.Chapter == chapter).ToList();

    /// <summary>All transitive prerequisites of <paramref name="id"/>, excluding itself. Unknown IDs have none.</summary>
    public IReadOnlySet<string> Ancestors(string id) => HopsToAncestors([id]).Keys.ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// Missed competencies that next week's lessons build on: missed ∩ ancestors(nextWeek) (FR-06, spec §5.1 A2).
    /// Nearest first (fewest prerequisite hops to a next-week competency, then teaching order), so the rule
    /// fallback can take the head of the list. A missed item that is itself taught next week is not a candidate.
    /// Unknown IDs are ignored.
    /// </summary>
    public IReadOnlyList<string> CandidateSet(IEnumerable<string> missed, IEnumerable<string> nextWeek)
    {
        var next = nextWeek.Where(Contains).ToHashSet(StringComparer.Ordinal);
        var hops = HopsToAncestors(next);
        return missed
            .Distinct(StringComparer.Ordinal)
            .Where(id => hops.ContainsKey(id) && !next.Contains(id))
            .OrderBy(id => hops[id])
            .ThenBy(id => _byId[id].Sequence)
            .ThenBy(id => id, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Breadth-first walk up the prerequisites; value = fewest hops from any start node.</summary>
    private Dictionary<string, int> HopsToAncestors(IEnumerable<string> starts)
    {
        var hops = new Dictionary<string, int>(StringComparer.Ordinal);
        var queue = new Queue<(string Id, int Hops)>(starts.Select(s => (s, 0)));
        while (queue.TryDequeue(out var current))
        {
            if (!_byId.TryGetValue(current.Id, out var competency))
            {
                continue;
            }

            foreach (var prerequisite in competency.Prerequisites)
            {
                if (hops.TryAdd(prerequisite, current.Hops + 1))
                {
                    queue.Enqueue((prerequisite, current.Hops + 1));
                }
            }
        }

        return hops;
    }
}
