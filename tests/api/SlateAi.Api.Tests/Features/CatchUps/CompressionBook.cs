using SlateAi.Api.Curriculum;

namespace SlateAi.Api.Tests.Features.CatchUps;

/// <summary>A slice of the Grade 3 graph with the real prerequisites (data/curriculum/grade3-maths.json).</summary>
internal static class CompressionBook
{
    public static readonly CurriculumGraph Graph = new(
    [
        Item("M3.PV.HTO", 2, 4),
        Item("M3.PV.EXPAND", 2, 5, "M3.PV.HTO"),
        Item("M3.ADD.NOCARRY", 4, 10, "M3.PV.HTO"),
        Item("M3.ADD.CARRY", 4, 11, "M3.ADD.NOCARRY", "M3.PV.EXPAND"),
        Item("M3.SUB.NOBORROW", 5, 13, "M3.PV.HTO"),
        Item("M3.SUB.CHECK", 5, 14, "M3.SUB.NOBORROW", "M3.ADD.NOCARRY"),
        Item("M3.SUB.BORROW", 5, 15, "M3.SUB.NOBORROW", "M3.PV.EXPAND"),
        Item("M3.SUB.WORD", 5, 16, "M3.SUB.BORROW"),
    ]);

    private static Competency Item(string id, int chapter, int sequence, params string[] prerequisites) =>
        new(id, $"ಕ {id}", $"Name of {id}", prerequisites, null, chapter, sequence, [], null);
}
