using SlateAi.Api.Features.Delivery;
using SlateAi.Api.Features.EveningMessages;
using SlateAi.Api.Features.TodaysMessage;
using SlateAi.Api.Features.WhatsAppChat;

namespace SlateAi.Api.Tests.Features.WhatsAppChat;

/// <summary>T-106: what goes into the chat, in which order, and the replies to taps. Pure.</summary>
public sealed class ChatLessonPlanTests
{
    private const string Token = "tok_abcdefghijklmnopqr";
    private static readonly ChatWording Wording = ChatWording.Load(Path.Combine(AppContext.BaseDirectory, ChatWording.File));
    private static readonly ChatWords En = Wording.For("en")!;
    private static readonly Guid Greeting = Guid.NewGuid();
    private static readonly Guid Disclosure = Guid.NewGuid();
    private static readonly Guid Concept = Guid.NewGuid();

    private static ChatLesson Lesson(ParentTodaysMessage? todays = null) => new(
        Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 11, 20), "en", "Ravi", "yes", false, DateTimeOffset.MaxValue,
        ["+919812345678"], true, new SlotValues("Subtraction with borrowing", "Nalikali Maths Part 1, page 4"),
        todays ?? new ParentTodaysMessage(
            "Take ten from the next place.", ["Make bundles of ten.", "Take one apart."], new ParentPicture(47, 52),
            new TodaysVideo("https://diksha.gov.in/play/x"), "Bring the sticks tomorrow."),
        ">", null, Greeting, Disclosure, Concept);

    [Fact]
    public void Greeting_then_lesson_audio_then_text_picture_question_and_reason_list()
    {
        var plan = ChatLessonPlan.Lesson(Lesson(), Token, En, joinDisclosure: true);

        Assert.Collection(
            plan,
            m => Assert.Equal([Greeting], Assert.IsType<ChatVoice>(m).ClipIds),
            m => Assert.Equal([Disclosure, Concept], Assert.IsType<ChatVoice>(m).ClipIds),
            m => Assert.Equal(
                "*Subtraction with borrowing*\nTake ten from the next place.\n\n" +
                "*Try this at home*\n1. Make bundles of ten.\n2. Take one apart.\n\n" +
                "Homework: Nalikali Maths Part 1, page 4\n\n" +
                "*Watch how it's taught*\nhttps://diksha.gov.in/play/x\nUses mobile data. You can skip it.\n\n" +
                "Bring the sticks tomorrow.",
                Assert.IsType<ChatText>(m).Body),
            m =>
            {
                var buttons = Assert.IsType<ChatButtons>(m);
                Assert.Contains("*47   ?   52*", buttons.Body);
                Assert.Contains("Ravi's turn", buttons.Body);
                Assert.Equal([">", "<", "="], buttons.Options.Select(o => o.Title));
                Assert.Equal($"pq:{Token}:=", buttons.Options[2].Id);
            },
            m =>
            {
                var list = Assert.IsType<ChatList>(m);
                Assert.Equal("We missed Ravi today. Is everything OK?", list.Body);
                Assert.Equal(["unwell", "travelOrWork", "festival", "neededAtHome", "other"], list.Rows.Select(r => r.Id.Split(':')[2]));
            });
    }

    [Fact]
    public void Without_joining_the_disclosure_and_the_lesson_are_two_voice_notes_in_a_row()
    {
        var plan = ChatLessonPlan.Lesson(Lesson(), Token, En, joinDisclosure: false);

        Assert.Equal(
            [[Greeting], [Disclosure], [Concept]],
            plan.OfType<ChatVoice>().Select(v => v.ClipIds.ToArray()));
    }

    [Fact]
    public void No_lesson_audio_without_the_disclosure_and_the_text_always_goes()
    {
        // As on the family page: the lesson clip only behind the AI disclosure (D-18).
        var lesson = Lesson() with { DisclosureClipId = null, GreetingClipId = null };

        var plan = ChatLessonPlan.Lesson(lesson, Token, En, joinDisclosure: true);

        Assert.Empty(plan.OfType<ChatVoice>());
        Assert.IsType<ChatText>(plan[0]);
    }

    [Fact]
    public void A_locked_picture_and_a_given_reason_are_not_asked_again()
    {
        var todays = Lesson().Todays! with { Picture = new ParentPicture(47, 52, ">", "<", Locked: true) };

        var plan = ChatLessonPlan.Lesson(Lesson(todays) with { Reason = "unwell" }, Token, En, joinDisclosure: true);

        Assert.DoesNotContain(plan, m => m is ChatButtons or ChatList);
    }

    [Fact]
    public void A_sum_offers_its_three_numbers_D116()
    {
        var todays = Lesson().Todays! with
        {
            Picture = new ParentPicture(248, 438, Kind: TodaysPicture.Add, Options: [676, 686, 696]),
        };

        var buttons = ChatLessonPlan.Lesson(Lesson(todays), Token, En, joinDisclosure: true).OfType<ChatButtons>().Single();

        Assert.Contains("*248 + 438 = ?*", buttons.Body);
        Assert.Equal(["676", "686", "696"], buttons.Options.Select(o => o.Title));
    }

    [Fact]
    public void The_reply_to_a_tap_never_says_right_or_wrong_D108()
    {
        Assert.Equal(
            "Ravi chose >. You can change it once: tap another answer above.",
            ChatLessonPlan.PictureReply(En, "Ravi", ">", (">", null)));
        Assert.Equal("Thank you. The teacher will see Ravi's answer.", ChatLessonPlan.PictureReply(En, "Ravi", "<", (">", "<")));
        Assert.Equal("Thank you. The teacher will see Ravi's answer.", ChatLessonPlan.PictureReply(En, "Ravi", "=", null));
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(-23.99, true)]
    [InlineData(-24, false)]
    [InlineData(-30, false)]
    public void The_window_is_24_hours_from_the_familys_message(double hours, bool open)
    {
        var now = new DateTimeOffset(2026, 11, 20, 13, 0, 0, TimeSpan.Zero);

        Assert.Equal(open, ChatLessonPlan.WindowOpen(now.AddHours(hours), now));
    }

    [Fact]
    public void Every_language_has_every_line_and_fits_Metas_limits()
    {
        Assert.Equal(["en", "hi", "kn", "ta", "te"], Wording.Languages.Keys.Order());
        foreach (var (language, words) in Wording.Languages)
        {
            Assert.Equal(ChatLessonPlan.ReasonCategories.Order(), words.Reasons.Keys.Order());
            Assert.All(words.Reasons.Values, title => Assert.True(title.Length <= 24, $"{language}: '{title}' is over 24"));
            Assert.True(words.ReasonButton.Length <= 20, language);
            Assert.NotEmpty(words.StopWords);
            Assert.All(
                new[] { words.Activity, words.Homework, words.Video, words.VideoData, words.PictureTitle, words.PictureCompare,
                    words.PictureSum, words.PictureAgain, words.PictureDone, words.ReasonQuestion, words.ReasonThanks, words.Stopped },
                line => Assert.False(string.IsNullOrWhiteSpace(line), language));
        }
    }

    [Theory]
    [InlineData("lesson:tok_1", "lesson", "tok_1", "")]
    [InlineData("pq:tok_1:>", "pq", "tok_1", ">")]
    [InlineData("pq:tok_1:686", "pq", "tok_1", "686")]
    [InlineData("rs:tok_1:festival", "rs", "tok_1", "festival")]
    public void Reply_ids_round_trip(string id, string kind, string token, string value) =>
        Assert.Equal((kind, token, value), ChatReplyIds.Parse(id));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Send it here")]
    [InlineData("lesson:")]
    [InlineData("lesson:tok:extra")]
    [InlineData("pq:tok_1")]
    [InlineData("other:tok_1:x")]
    public void Anything_else_is_not_a_reply_id(string? id) => Assert.Null(ChatReplyIds.Parse(id));
}
