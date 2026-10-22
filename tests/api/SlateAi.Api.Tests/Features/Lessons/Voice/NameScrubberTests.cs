using SlateAi.Api.Features.Lessons.Voice;

namespace SlateAi.Api.Tests.Features.Lessons.Voice;

public sealed class NameScrubberTests
{
    private static readonly string[] Children = ["Ravi", "Bhavana", "Chetan", "Manjunath", "Sai Teja", "Deepa"];

    [Theory]
    [InlineData("ಇಂದು ರವಿ ಬಂದಿರಲಿಲ್ಲ", "ಇಂದು {child} ಬಂದಿರಲಿಲ್ಲ")]
    [InlineData("ರವಿಗೆ ಮತ್ತೆ ಹೇಳಬೇಕು", "{child} ಮತ್ತೆ ಹೇಳಬೇಕು")]
    [InlineData("ಭಾವನಾ ಮತ್ತು ಚೇತನ್ ಬೋರ್ಡ್ ಮೇಲೆ ಮಾಡಿದರು", "{child} ಮತ್ತು {child} ಬೋರ್ಡ್ ಮೇಲೆ ಮಾಡಿದರು")]
    [InlineData("ಮಂಜುನಾಥನ ಉತ್ತರ ಸರಿ", "{child} ಉತ್ತರ ಸರಿ")]
    [InlineData("today Ravi and DEEPA solved 27 + 15", "today {child} and {child} solved 27 + 15")]
    [InlineData("ಸಾಯಿ ತೇಜ ಬಂದ", "{child} {child} ಬಂದ")]
    public void Replaces_child_names_in_kannada_and_latin_script(string transcript, string expected) =>
        Assert.Equal(expected, NameScrubber.Scrub(transcript, Children, "Shobha"));

    [Fact]
    public void Replaces_the_teachers_name() =>
        Assert.Equal("{teacher} ಟೀಚರ್ ಹೇಳಿದರು", NameScrubber.Scrub("ಶೋಭಾ ಟೀಚರ್ ಹೇಳಿದರು", Children, "Shobha"));

    [Theory]
    [InlineData("ದಶಕ ವರ್ಗಾವಣೆ ಸಂಕಲನ ಮಾಡಿದೆವು")]
    [InlineData("ಕಡ್ಡಿಗಳ ಕಟ್ಟು ಬಳಸಿದೆವು 27 + 15")]
    [InlineData("we did subtraction with borrowing")]
    public void Leaves_lesson_words_alone(string transcript) =>
        Assert.Equal(transcript, NameScrubber.Scrub(transcript, Children, "Shobha"));

    [Fact]
    public void A_longer_word_that_only_starts_like_a_name_is_kept() =>
        Assert.Equal("ravindra", NameScrubber.Scrub("ravindra", Children, null));

    [Theory]
    [InlineData("Bhavana", "ಭಾವನಾ")]
    [InlineData("Chetan", "ಚೇತನ್")]
    [InlineData("Manjunath", "ಮಂಜುನಾಥ್")]
    [InlineData("Shobha", "ಶೋಭಾ")]
    public void Latin_and_kannada_spellings_meet_in_the_loose_form(string latin, string kannada) =>
        Assert.Equal(NameScrubber.LooseForm(latin), NameScrubber.LooseForm(kannada));
}
