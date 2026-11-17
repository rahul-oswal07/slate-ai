using SlateAi.Api.Curriculum;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Curricula;
using SlateAi.Api.Features.TextbookImport;

namespace SlateAi.Api.Tests.Features.Curricula;

/// <summary>T-147: a draft lesson keeps passing the curriculum rules through every edit, and only a clean draft publishes.</summary>
public sealed class DraftRulesTests
{
    [Fact]
    public void An_edit_changes_the_lesson_and_its_extras_but_never_its_id_chapter_or_order()
    {
        var draft = Draft();
        var (lesson, errors) = DraftRules.Apply(draft, draft.Lessons[1], Edit() with
        {
            NameEn = "  Adding with a carry  ",
            NameTe = " ",
            KeyWordsKn = ["ಕೂಡು", "ಕೂಡು", " "],
            Page = 35,
            Prerequisites = ["M3.NUM.TENS"],
            Practice = new PracticeRule("none", 2),
            Exercises = new ExerciseList(Add: [[248, 438]]),
            Description = "The child adds and carries a ten.",
            Examples = ["36 + 47: 13 ones make a ten and 3."],
            Pictures = [new PictureNote(7, 34, "top", " Sticks ", "Tens")],
            Reviewed = true,
        });

        Assert.Empty(errors);
        Assert.Equal(("M3.ADD.CARRY", 2, 2), (lesson!.Competency.Id, lesson.Competency.Chapter, lesson.Competency.Sequence));
        Assert.Equal("Adding with a carry", lesson.Competency.NameEn);
        Assert.Null(lesson.Competency.NameTe);
        Assert.Equal(["ಕೂಡು"], lesson.Competency.KeyWordsKn!);
        Assert.Equal(new TextbookPage("textbook", 1, 35), lesson.Competency.TextbookPage);
        Assert.Null(lesson.Competency.Practice);
        Assert.Equal("Sticks", lesson.Extras!.Pictures[0].Description);
        Assert.Equal("The child adds and carries a ten.", lesson.Extras.Description);
        Assert.Equal(CurriculumReviewStatuses.Reviewed, lesson.ReviewStatus);
    }

    [Fact]
    public void A_later_or_unknown_prerequisite_a_broken_exercise_and_text_that_breaks_the_rules_are_refused_by_field()
    {
        var draft = Draft();

        var (lesson, errors) = DraftRules.Apply(draft, draft.Lessons[0], Edit() with
        {
            NameKn = "",
            Prerequisites = ["M3.ADD.CARRY", "M3.NOPE"],
            Exercises = new ExerciseList(Add: [[600, 500]]),
            Description = new string('x', 701),
            Page = 0,
        });

        Assert.Null(lesson);
        Assert.Equal(["description", "lesson", "page", "prerequisites"], errors.Keys.Order());
        Assert.Contains(errors["lesson"], e => e.Contains("nameKn"));
        Assert.Contains(errors["lesson"], e => e.Contains("999"));
    }

    [Fact]
    public void A_draft_publishes_only_with_every_lesson_reviewed_and_no_rule_broken()
    {
        var draft = Draft();
        Assert.Equal(["M3.ADD.CARRY: not reviewed yet."], DraftRules.PublishProblems(draft));

        var reviewed = draft with { Lessons = draft.Lessons.Select(l => l with { ReviewStatus = CurriculumReviewStatuses.Reviewed }).ToList() };
        Assert.Empty(DraftRules.PublishProblems(reviewed));

        var noChapter = reviewed with { Chapters = [reviewed.Chapters[0]] };
        Assert.Contains(DraftRules.PublishProblems(noChapter), p => p.Contains("chapter 2 is not in this file's chapters"));
        Assert.Equal(["The curriculum has no lessons."], DraftRules.PublishProblems(draft with { Lessons = [] }));
    }

    [Fact]
    public void Moving_a_class_loses_the_lessons_it_used_that_the_curriculum_lacks_and_answers_are_code_s()
    {
        Assert.Equal(["M3.A", "M3.C"], DraftRules.Missing(["M3.C", "M3.B", "M3.A", "M3.C"], new HashSet<string> { "M3.B" }));
        Assert.Empty(DraftRules.Missing([], new HashSet<string>()));

        var answers = DraftRules.Answers(new ExerciseList([[48, 33], [5, 5]], [[3, 38, 64]], [[248, 438]], [[872, 586]]));
        Assert.Equal([">", "="], answers.Compare);
        Assert.Equal([3, 64], answers.Extremes[0]);
        Assert.Equal([686], answers.Add);
        Assert.Equal([286], answers.Sub);
    }

    internal static EditDraftLessonRequest Edit() => new(
        Guid.NewGuid(), "ಕೂಡು", "Addition with carrying", null, null, null, ["ಕೂಡು"], 34, [], null, null, ["27 + 15 = ?"],
        "Adds.", ["1 + 1 is 2."], [], [], false);

    internal static CurriculumDraft Draft() => new(
        new StoredCurriculum
        {
            Id = Guid.NewGuid(), Title = "Grade 3 maths", Grade = 3, Subject = "maths", Medium = "kn",
            Status = CurriculumStatuses.Draft, Chapters = "[]",
        },
        "textbook",
        1,
        [],
        [new Chapter(1, "ಸಂಖ್ಯೆ", "Numbers"), new Chapter(2, "ಕೂಡು", "Addition")],
        [
            new DraftLessonRow(
                new Competency("M3.NUM.TENS", "ಹತ್ತು", "Tens and ones", [], null, 1, 1, [], new TextbookPage("textbook", 1, 12)),
                null, CurriculumReviewStatuses.Reviewed, []),
            new DraftLessonRow(
                new Competency("M3.ADD.CARRY", "ಕೂಡು", "Addition with carrying", [], null, 2, 2, [], new TextbookPage("textbook", 1, 34)),
                new LessonExtras([7], [34], null, [], [], []), CurriculumReviewStatuses.NeedsReview, ["A flag"]),
        ],
        []);
}
