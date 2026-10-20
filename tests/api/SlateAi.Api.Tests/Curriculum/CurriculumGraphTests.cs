using SlateAi.Api.Curriculum;

namespace SlateAi.Api.Tests.Curriculum;

public sealed class CurriculumGraphTests
{
    // PV.ONES → PV.TENS → ADD.NOCARRY → ADD.CARRY → SUB.BORROW, with a diamond: ADD.CARRY also needs PV.TENS
    // directly, and SUB.BORROW needs SUB.NOBORROW (which needs PV.TENS). MEAS.LEN is unrelated.
    private static readonly CurriculumGraph Graph = new(
    [
        C("M3.PV.ONES", chapter: 1, sequence: 1),
        C("M3.PV.TENS", chapter: 1, sequence: 2, "M3.PV.ONES"),
        C("M3.ADD.NOCARRY", chapter: 2, sequence: 3, "M3.PV.TENS"),
        C("M3.ADD.CARRY", chapter: 2, sequence: 4, "M3.ADD.NOCARRY", "M3.PV.TENS"),
        C("M3.SUB.NOBORROW", chapter: 3, sequence: 5, "M3.PV.TENS"),
        C("M3.SUB.BORROW", chapter: 3, sequence: 6, "M3.SUB.NOBORROW", "M3.ADD.CARRY"),
        C("M3.MEAS.LEN", chapter: 4, sequence: 7),
    ]);

    [Fact]
    public void Ancestors_are_transitive()
    {
        Assert.Equal(
            ["M3.ADD.NOCARRY", "M3.PV.ONES", "M3.PV.TENS"],
            Graph.Ancestors("M3.ADD.CARRY").Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Ancestors_through_several_paths_are_listed_once_and_exclude_the_competency_itself()
    {
        var ancestors = Graph.Ancestors("M3.SUB.BORROW");

        Assert.Equal(5, ancestors.Count);
        Assert.DoesNotContain("M3.SUB.BORROW", ancestors);
        Assert.DoesNotContain("M3.MEAS.LEN", ancestors);
    }

    [Fact]
    public void A_root_or_unknown_competency_has_no_ancestors()
    {
        Assert.Empty(Graph.Ancestors("M3.PV.ONES"));
        Assert.Empty(Graph.Ancestors("M3.NOPE"));
    }

    [Fact]
    public void Candidate_set_keeps_missed_items_that_next_week_builds_on()
    {
        var candidates = Graph.CandidateSet(
            missed: ["M3.ADD.NOCARRY", "M3.MEAS.LEN", "M3.PV.TENS"],
            nextWeek: ["M3.ADD.CARRY"]);

        // Both are direct prerequisites, so teaching order breaks the tie.
        Assert.Equal(["M3.PV.TENS", "M3.ADD.NOCARRY"], candidates);
    }

    [Fact]
    public void Candidate_set_is_empty_when_nothing_missed_blocks_next_week()
    {
        Assert.Empty(Graph.CandidateSet(missed: ["M3.MEAS.LEN"], nextWeek: ["M3.SUB.BORROW"]));
    }

    [Fact]
    public void Candidate_set_lists_the_nearest_prerequisite_first()
    {
        // From SUB.BORROW: SUB.NOBORROW and ADD.CARRY are 1 hop, ADD.NOCARRY and PV.TENS 2, PV.ONES 3.
        var candidates = Graph.CandidateSet(
            missed: ["M3.PV.ONES", "M3.ADD.NOCARRY", "M3.ADD.CARRY", "M3.SUB.NOBORROW"],
            nextWeek: ["M3.SUB.BORROW"]);

        Assert.Equal(["M3.ADD.CARRY", "M3.SUB.NOBORROW", "M3.ADD.NOCARRY", "M3.PV.ONES"], candidates);
    }

    [Fact]
    public void Candidate_set_uses_the_shortest_path_when_there_are_several()
    {
        // PV.TENS is 1 hop from ADD.CARRY directly, 2 via ADD.NOCARRY; PV.ONES is 2 hops.
        var candidates = Graph.CandidateSet(missed: ["M3.PV.ONES", "M3.PV.TENS"], nextWeek: ["M3.ADD.CARRY"]);

        Assert.Equal(["M3.PV.TENS", "M3.PV.ONES"], candidates);
    }

    [Fact]
    public void Candidate_set_combines_several_next_week_competencies()
    {
        var candidates = Graph.CandidateSet(
            missed: ["M3.ADD.NOCARRY", "M3.PV.ONES"],
            nextWeek: ["M3.ADD.CARRY", "M3.SUB.NOBORROW"]);

        Assert.Equal(["M3.ADD.NOCARRY", "M3.PV.ONES"], candidates);
    }

    [Fact]
    public void A_missed_item_taught_again_next_week_is_not_a_candidate()
    {
        var candidates = Graph.CandidateSet(
            missed: ["M3.ADD.CARRY", "M3.ADD.NOCARRY"],
            nextWeek: ["M3.ADD.CARRY", "M3.SUB.BORROW"]);

        Assert.Equal(["M3.ADD.NOCARRY"], candidates);
    }

    [Fact]
    public void Candidate_set_ignores_unknown_ids_and_duplicates()
    {
        var candidates = Graph.CandidateSet(
            missed: ["M3.NOPE", "M3.PV.TENS", "M3.PV.TENS"],
            nextWeek: ["M3.ALSO.NOPE", "M3.ADD.NOCARRY"]);

        Assert.Equal(["M3.PV.TENS"], candidates);
    }

    [Fact]
    public void Candidate_set_of_empty_inputs_is_empty()
    {
        Assert.Empty(Graph.CandidateSet([], ["M3.ADD.CARRY"]));
        Assert.Empty(Graph.CandidateSet(["M3.PV.TENS"], []));
    }

    [Fact]
    public void Chapter_lists_competencies_in_teaching_order()
    {
        var graph = new CurriculumGraph(
        [
            C("M3.ADD.CARRY", chapter: 2, sequence: 4),
            C("M3.PV.TENS", chapter: 1, sequence: 2),
            C("M3.ADD.NOCARRY", chapter: 2, sequence: 3),
        ]);

        Assert.Equal(["M3.ADD.NOCARRY", "M3.ADD.CARRY"], graph.InChapter(2).Select(c => c.Id));
        Assert.Empty(graph.InChapter(9));
        Assert.Equal(["M3.PV.TENS", "M3.ADD.NOCARRY", "M3.ADD.CARRY"], graph.All.Select(c => c.Id));
        Assert.True(graph.Contains("M3.PV.TENS"));
        Assert.Null(graph.Get("M3.NOPE"));
    }

    private static Competency C(string id, int chapter, int sequence, params string[] prerequisites) =>
        new(id, $"{id} kn", $"{id} en", prerequisites, null, chapter, sequence, [], null);
}
