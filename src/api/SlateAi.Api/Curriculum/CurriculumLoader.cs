using System.Text.Json;
using System.Text.RegularExpressions;

namespace SlateAi.Api.Curriculum;

/// <summary>A curriculum file is invalid. <see cref="Errors"/> lists every problem found, not just the first.</summary>
public sealed class CurriculumException(IReadOnlyList<string> errors)
    : Exception("Invalid curriculum:" + Environment.NewLine + string.Join(Environment.NewLine, errors))
{
    public IReadOnlyList<string> Errors { get; } = errors;
}

/// <summary>Parses and validates curriculum JSON into a <see cref="CurriculumGraph"/>.</summary>
public static partial class CurriculumLoader
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly string[] Books = ["textbook", "workbook"];

    public static CurriculumGraph LoadDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return CurriculumGraph.Empty;
        }

        var files = Directory.GetFiles(directory, "*.json").Order(StringComparer.Ordinal)
            .Select(path => (Path.GetFileName(path), File.ReadAllText(path)));
        return Load(files);
    }

    /// <param name="files">(file name for error messages, JSON content)</param>
    public static CurriculumGraph Load(IEnumerable<(string Name, string Json)> files)
    {
        var errors = new List<string>();
        var competencies = new List<Competency>();
        var chapters = new Dictionary<int, Chapter>();
        var seen = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (name, json) in files)
        {
            CurriculumFile? file;
            try
            {
                file = JsonSerializer.Deserialize<CurriculumFile>(json, Json);
            }
            catch (JsonException e)
            {
                errors.Add($"{name}: not valid JSON ({e.Message})");
                continue;
            }

            foreach (var chapter in file?.Chapters ?? [])
            {
                if (string.IsNullOrWhiteSpace(chapter.NameKn) || string.IsNullOrWhiteSpace(chapter.NameEn))
                {
                    errors.Add($"{name}: chapter {chapter.Number}: nameKn and nameEn are required");
                }

                if (!chapters.TryAdd(chapter.Number, chapter))
                {
                    errors.Add($"{name}: chapter {chapter.Number}: listed twice");
                }
            }

            var declared = file?.Chapters?.Select(c => c.Number).ToHashSet();
            foreach (var raw in file?.Competencies ?? [])
            {
                var c = Normalise(raw);
                errors.AddRange(Check(c).Select(e => $"{name}: {c.Id}: {e}"));
                if (declared is not null && !declared.Contains(c.Chapter))
                {
                    errors.Add($"{name}: {c.Id}: chapter {c.Chapter} is not in this file's chapters");
                }

                if (!seen.TryAdd(c.Id, name))
                {
                    errors.Add($"{name}: {c.Id}: duplicate ID (also in {seen[c.Id]})");
                    continue;
                }

                competencies.Add(c);
            }
        }

        var ids = seen.Keys.ToHashSet(StringComparer.Ordinal);
        foreach (var c in competencies)
        {
            errors.AddRange(c.Prerequisites
                .Where(p => p != c.Id && !ids.Contains(p))
                .Select(p => $"{seen[c.Id]}: {c.Id}: unknown prerequisite {p}"));
        }

        errors.AddRange(FindCycles(competencies).Select(cycle => $"prerequisite cycle: {string.Join(" → ", cycle)}"));

        return errors.Count > 0 ? throw new CurriculumException(errors) : new CurriculumGraph(competencies, chapters.Values);
    }

    // JSON can omit arrays or send nulls; the graph relies on non-null lists.
    private static Competency Normalise(Competency c) => c with
    {
        Id = c.Id ?? "",
        NameKn = c.NameKn ?? "",
        NameEn = c.NameEn ?? "",
        Prerequisites = c.Prerequisites ?? [],
        Examples = c.Examples ?? [],
    };

    private static IEnumerable<string> Check(Competency c)
    {
        if (!IdFormat().IsMatch(c.Id))
        {
            yield return "ID must look like M3.ADD.CARRY";
        }

        if (string.IsNullOrWhiteSpace(c.NameEn))
        {
            yield return "nameEn is required";
        }

        if (string.IsNullOrWhiteSpace(c.NameKn))
        {
            yield return "nameKn is required";
        }

        if (c.NameTe is not null && string.IsNullOrWhiteSpace(c.NameTe)
            || c.NameTa is not null && string.IsNullOrWhiteSpace(c.NameTa))
        {
            yield return "nameTe and nameTa can be left out but not blank";
        }

        if (c.KeyWordsKn is { } words && (words.Count == 0 || words.Any(string.IsNullOrWhiteSpace)))
        {
            yield return "keyWordsKn can be left out but not empty or blank";
        }

        if (c.Practice is { } rule)
        {
            if (!PracticeRule.Kinds.Contains(rule.Kind))
            {
                yield return $"practice.kind must be one of {string.Join(", ", PracticeRule.Kinds)}";
            }

            if (rule.Digits is < 2 or > 4)
            {
                yield return "practice.digits must be 2, 3 or 4";
            }

            if (rule.Kind == "add" && rule.Carry is not ("none" or "ones" or "any")
                || rule.Kind == "sub" && rule.Borrow is not ("none" or "tens"))
            {
                yield return "practice: add needs carry none|ones|any, sub needs borrow none|tens";
            }

            if (rule.Kind is "add" or "sub" && rule.Digits > 3)
            {
                yield return "practice: add and sub stay within 3 digits (≤ 999)";
            }
        }

        if (c.Prerequisites.Contains(c.Id))
        {
            yield return "lists itself as a prerequisite";
        }

        if (c.TextbookPage is { } page)
        {
            if (!Books.Contains(page.Book))
            {
                yield return "textbookPage.book must be \"textbook\" or \"workbook\"";
            }

            if (page.Page <= 0 || page.Part <= 0)
            {
                yield return "textbookPage part and page must be positive";
            }
        }
    }

    /// <summary>Depth-first search; returns each cycle found as a path that starts and ends on the same ID.</summary>
    private static List<List<string>> FindCycles(IReadOnlyList<Competency> competencies)
    {
        var byId = competencies.ToDictionary(c => c.Id, StringComparer.Ordinal);
        var state = new Dictionary<string, bool>(StringComparer.Ordinal); // false = on the stack, true = done
        var stack = new List<string>();
        var cycles = new List<List<string>>();

        void Visit(string id)
        {
            state[id] = false;
            stack.Add(id);
            foreach (var p in byId[id].Prerequisites.Where(p => p != id && byId.ContainsKey(p)))
            {
                if (!state.TryGetValue(p, out var done))
                {
                    Visit(p);
                }
                else if (!done)
                {
                    cycles.Add([.. stack[stack.IndexOf(p)..], p]);
                }
            }

            stack.RemoveAt(stack.Count - 1);
            state[id] = true;
        }

        foreach (var c in competencies.Where(c => !state.ContainsKey(c.Id)))
        {
            Visit(c.Id);
        }

        return cycles;
    }

    [GeneratedRegex(@"^M[1-5](\.[A-Z0-9]+)+$")]
    private static partial Regex IdFormat();
}
