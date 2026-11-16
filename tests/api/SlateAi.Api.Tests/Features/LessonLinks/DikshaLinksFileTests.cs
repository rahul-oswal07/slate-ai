using System.Text.Json;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.LessonLinks;

namespace SlateAi.Api.Tests.Features.LessonLinks;

/// <summary>
/// <c>data/lesson-links/diksha-kn.json</c> (T-138): the DIKSHA link per lesson that scripts/set-lesson-links.mjs puts on
/// /admin/lesson-links. It must cover the curriculum exactly and pass the admin page's own rule.
/// </summary>
public sealed class DikshaLinksFileTests
{
    private static readonly string Dir = AppContext.BaseDirectory;

    private static JsonElement Lessons() =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(Dir, "data", "lesson-links", "diksha-kn.json"))).RootElement.GetProperty("lessons");

    [Fact]
    public void Every_lesson_of_the_curriculum_is_listed_once()
    {
        var curriculum = CurriculumLoader.LoadDirectory(Path.Combine(Dir, "data", "curriculum")).All.Select(c => c.Id).Order().ToList();
        var listed = Lessons().EnumerateArray().Select(l => l.GetProperty("competencyId").GetString()!).Order().ToList();

        Assert.Equal(curriculum, listed);
    }

    [Fact]
    public void Every_link_is_a_diksha_page_the_admin_page_accepts_and_every_gap_says_why()
    {
        foreach (var lesson in Lessons().EnumerateArray())
        {
            var id = lesson.GetProperty("competencyId").GetString();
            if (lesson.GetProperty("url").ValueKind == JsonValueKind.Null)
            {
                Assert.False(string.IsNullOrWhiteSpace(lesson.GetProperty("reason").GetString()), id);
                continue;
            }

            var url = lesson.GetProperty("url").GetString();
            var (normalised, error) = LessonLinkRules.Normalise(url);
            Assert.True(error is null, $"{id}: {error}");
            Assert.Equal(url, normalised);
            Assert.StartsWith("https://diksha.gov.in/play/content/do_", url);
            Assert.Contains(lesson.GetProperty("match").GetString(), new[] { "topic", "chapter" });
            Assert.Contains(lesson.GetProperty("kind").GetString(), new[] { "video", "activity" });
        }
    }
}
