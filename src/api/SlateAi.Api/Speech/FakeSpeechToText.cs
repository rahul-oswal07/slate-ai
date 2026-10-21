namespace SlateAi.Api.Speech;

/// <summary><c>Ai:Provider=Fake</c>: a fixed Kannada lesson note, so capture works without a Sarvam key.</summary>
public sealed class FakeSpeechToText : ISpeechToText
{
    // "Today we learnt addition with carrying. We added 27 and 15 using bundles of sticks."
    public const string Transcript = "ಇಂದು ನಾವು ದಶಕ ವರ್ಗಾವಣೆಯೊಂದಿಗೆ ಸಂಕಲನ ಕಲಿತೆವು. ಕಡ್ಡಿಗಳ ಕಟ್ಟುಗಳಿಂದ 27 ಮತ್ತು 15 ಕೂಡಿಸಿದೆವು.";

    public Task<Transcription> TranscribeAsync(
        Stream audio,
        string fileName,
        string contentType,
        string languageCode,
        IReadOnlyList<string> keyTerms,
        CancellationToken cancellationToken) =>
        Task.FromResult(new Transcription(Transcript, languageCode, null, Guid.Empty));
}
