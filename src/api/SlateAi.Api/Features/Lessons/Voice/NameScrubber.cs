using System.Text;
using System.Text.RegularExpressions;

namespace SlateAi.Api.Features.Lessons.Voice;

/// <summary>
/// Replaces the class's child names and the teacher's name in a transcript with <c>{child}</c> / <c>{teacher}</c>
/// before anything goes to Claude (guardrail 1, NFR-05). Names are stored in Latin script; transcripts are mostly
/// Kannada script, so both sides are compared in a loose phonetic form (vowel length, aspiration and
/// retroflex/dental differences ignored) and common Kannada case endings are allowed. It errs towards
/// over-scrubbing: an ordinary word that sounds like a child's name is also replaced.
/// </summary>
public static partial class NameScrubber
{
    public const string Child = "{child}";
    public const string Teacher = "{teacher}";

    // Loose forms of endings like -ನ, -ನಿಗೆ, -ಗೆ, -ಳು, -ಯನ್ನು on a name.
    private static readonly string[] Endings =
        ["", "a", "u", "i", "e", "na", "nu", "ge", "ige", "nige", "ya", "yu", "la", "lu", "li", "ra", "ru", "annu", "nannu", "yannu", "rige", "lige", "inda", "ninda"];

    public static string Scrub(string transcript, IEnumerable<string> childNames, string? teacherName)
    {
        var children = Loose(childNames);
        var teacher = Loose(teacherName is null ? [] : [teacherName]);
        return Word().Replace(transcript, match =>
        {
            var word = LooseForm(match.Value);
            return Matches(word, teacher) ? Teacher : Matches(word, children) ? Child : match.Value;
        });
    }

    private static bool Matches(string word, IReadOnlySet<string> names) =>
        word.Length > 0 && names.Any(name => word.StartsWith(name, StringComparison.Ordinal) && IsEnding(word[name.Length..]));

    // A name ending in a consonant takes a linking "a" before the ending: ಮಂಜುನಾಥ್ → ಮಂಜುನಾಥನ ("manjunat" + "ana").
    private static bool IsEnding(string rest) =>
        Endings.Contains(rest) || (rest.StartsWith('a') && Endings.Contains(rest[1..]));

    /// <summary>Each name word of 3+ letters ("Sai Teja" → "sai", "teja").</summary>
    private static HashSet<string> Loose(IEnumerable<string> names) =>
        names.SelectMany(n => n.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Select(LooseForm)
            .Where(n => n.Length >= 3)
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>A loose Latin form of a Latin or Kannada word, e.g. "Bhavana" and "ಭಾವನಾ" both → "bavana".</summary>
    public static string LooseForm(string word)
    {
        var latin = new StringBuilder();
        for (var i = 0; i < word.Length; i++)
        {
            var c = word[i];
            if (KannadaConsonants.TryGetValue(c, out var consonant))
            {
                latin.Append(consonant);
                var next = i + 1 < word.Length ? word[i + 1] : '\0';
                // The inherent vowel, unless a vowel sign or virama replaces it (ಂ and ಃ follow it: ಮಂ = "man").
                if (next != Virama && !(KannadaSigns.ContainsKey(next) && next is not ('ಂ' or 'ಃ')))
                {
                    latin.Append('a');
                }
            }
            else if (KannadaSigns.TryGetValue(c, out var sign) || KannadaVowels.TryGetValue(c, out sign))
            {
                latin.Append(sign);
            }
            else if (char.IsAsciiLetter(c))
            {
                latin.Append(char.ToLowerInvariant(c));
            }
        }

        var s = latin.ToString();
        foreach (var (from, to) in LatinFolds)
        {
            s = s.Replace(from, to, StringComparison.Ordinal);
        }

        return s;
    }

    private const char Virama = '್';

    private static readonly Dictionary<char, string> KannadaVowels = new()
    {
        ['ಅ'] = "a", ['ಆ'] = "a", ['ಇ'] = "i", ['ಈ'] = "i", ['ಉ'] = "u", ['ಊ'] = "u", ['ಋ'] = "ru",
        ['ಎ'] = "e", ['ಏ'] = "e", ['ಐ'] = "ai", ['ಒ'] = "o", ['ಓ'] = "o", ['ಔ'] = "au",
    };

    // Vowel signs, virama (no vowel), anusvara (n) and visarga.
    private static readonly Dictionary<char, string> KannadaSigns = new()
    {
        ['ಾ'] = "a", ['ಿ'] = "i", ['ೀ'] = "i", ['ು'] = "u", ['ೂ'] = "u", ['ೃ'] = "ru", ['ೆ'] = "e", ['ೇ'] = "e",
        ['ೈ'] = "ai", ['ೊ'] = "o", ['ೋ'] = "o", ['ೌ'] = "au", [Virama] = "", ['ಂ'] = "n", ['ಃ'] = "h",
    };

    private static readonly Dictionary<char, string> KannadaConsonants = new()
    {
        ['ಕ'] = "k", ['ಖ'] = "k", ['ಗ'] = "g", ['ಘ'] = "g", ['ಙ'] = "n", ['ಚ'] = "c", ['ಛ'] = "c", ['ಜ'] = "j",
        ['ಝ'] = "j", ['ಞ'] = "n", ['ಟ'] = "t", ['ಠ'] = "t", ['ಡ'] = "d", ['ಢ'] = "d", ['ಣ'] = "n", ['ತ'] = "t",
        ['ಥ'] = "t", ['ದ'] = "d", ['ಧ'] = "d", ['ನ'] = "n", ['ಪ'] = "p", ['ಫ'] = "p", ['ಬ'] = "b", ['ಭ'] = "b",
        ['ಮ'] = "m", ['ಯ'] = "y", ['ರ'] = "r", ['ಱ'] = "r", ['ಲ'] = "l", ['ವ'] = "v", ['ಶ'] = "s", ['ಷ'] = "s",
        ['ಸ'] = "s", ['ಹ'] = "h", ['ಳ'] = "l",
    };

    // Applied in order to the Latin form, so English spellings meet the Kannada ones.
    private static readonly (string From, string To)[] LatinFolds =
    [
        ("sh", "s"), ("ch", "c"), ("th", "t"), ("dh", "d"), ("bh", "b"), ("kh", "k"), ("gh", "g"), ("ph", "p"),
        ("jh", "j"), ("aa", "a"), ("ee", "i"), ("ii", "i"), ("oo", "u"), ("uu", "u"), ("w", "v"), ("z", "j"),
        ("q", "k"), ("f", "p"), ("x", "ks"), ("ayi", "ai"), // ಸಾಯಿ = "Sai"
    ];

    [GeneratedRegex(@"[\p{L}\p{M}]+")]
    private static partial Regex Word();
}
