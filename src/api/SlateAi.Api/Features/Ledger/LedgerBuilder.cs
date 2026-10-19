namespace SlateAi.Api.Features.Ledger;

/// <summary>Pure ledger computation (FR-05): attendance × lessons, plus absence spells and return speed.</summary>
public static class LedgerBuilder
{
    public static LedgerResponse Build(Guid classId, LedgerData data)
    {
        var register = data.Register.ToLookup(r => r.ChildId);
        var lessons = data.Lessons.OrderBy(l => l.Date).ToLookup(l => l.Date);

        var allSpells = new List<AbsenceSpell>();
        var rows = data.Children.OrderBy(c => c.RollNo).Select(child =>
        {
            var days = register[child.Id].ToList();
            var spells = AbsenceSpells.For(days.Select(d => (d.Date, d.Present)));
            allSpells.AddRange(spells);
            var missed = days
                .Where(d => !d.Present)
                .OrderBy(d => d.Date)
                .SelectMany(d => lessons[d.Date])
                .Select(l => new MissedLesson(l.Id, l.Date, l.CompetencyIds, "missed"))
                .ToList();
            return new LedgerChild(
                child.Id, child.RollNo, child.FirstName, AbsenceSpells.CurrentDays(spells), missed, spells);
        }).ToList();

        DateOnly? latest = data.Register.Count > 0 ? data.Register.Max(r => r.Date) : null;
        var returnSpeed = latest is { } asOf ? AbsenceSpells.Summarise(allSpells, asOf) : null;

        return new LedgerResponse(classId, latest, returnSpeed, rows);
    }
}
