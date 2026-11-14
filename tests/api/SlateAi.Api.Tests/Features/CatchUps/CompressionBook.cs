using SlateAi.Api.Curriculum;

namespace SlateAi.Api.Tests.Features.CatchUps;

/// <summary>
/// A small Grade 3-shaped graph with fixed prerequisites, so these tests don't move when the book changes. It was
/// cut from the old edition; the 2026-27 file (data/curriculum/grade3-maths.json, T-81) orders lessons differently.
/// </summary>
internal static class CompressionBook
{
    public static readonly CurriculumGraph Graph = new(
    [
        Item("M3.PV.HTO", 2, 4),
        Item("M3.PV.EXPAND", 2, 5, "M3.PV.HTO"),
        Item("M3.ADD.NOCARRY", 4, 10, "M3.PV.HTO"),
        Item("M3.ADD.CARRY", 4, 11, "M3.ADD.NOCARRY", "M3.PV.EXPAND"),
        Item("M3.SUB.NOBORROW", 5, 13, "M3.PV.HTO"),
        Item("M3.SUB.MAKE", 5, 14, "M3.SUB.NOBORROW", "M3.ADD.NOCARRY"),
        Item("M3.SUB.BORROW", 5, 15, "M3.SUB.NOBORROW", "M3.PV.EXPAND"),
        Item("M3.SUB.WORD", 5, 16, "M3.SUB.BORROW"),
    ]);

    public static Competency Item(string id, int chapter, int sequence, params string[] prerequisites) =>
        new(id, $"ಕ {id}", $"Name of {id}", prerequisites, null, chapter, sequence, [], null,
            Practice: new PracticeRule("count", 3));

    /// <summary>A lesson code can't make sums for yet (D-89), like the Nalikali bridge lesson (T-87).</summary>
    public static Competency WithoutPractice(string id, int chapter, int sequence, params string[] prerequisites) =>
        Item(id, chapter, sequence, prerequisites) with { Practice = null };
}
