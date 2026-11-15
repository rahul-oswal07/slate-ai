using SlateAi.Api.Curriculum;

namespace SlateAi.Api.Features.Parent;

/// <summary>One audio segment as the parent page plays it: the URL works with the link token, no sign-in.</summary>
public sealed record ParentAudioSegment(string Kind, string Url, double Seconds);

public sealed record ParentAudio(IReadOnlyList<ParentAudioSegment> Segments, double TotalSeconds);

/// <summary>"Read the words" (P1): the disclosure as written, then A3's script (no names in it, D-50).</summary>
public sealed record ParentTranscript(string DisclosureText, string Script, IReadOnlyList<string> KeyWordsKn);

/// <summary>P2: the number the sticks picture starts from (code-made) and A3's words around it.</summary>
public sealed record ParentActivity(string Question, int Number, string? Instruction, string? ObjectOption);

/// <summary>One picture on P3. <see cref="Id"/> is its position, already shuffled by code.</summary>
public sealed record ParentOption(int Id, int Number);

/// <summary>P3's question. The correct answer's position is not here: it stays on the server (C-12).</summary>
public sealed record ParentCheck(string Question, string? Prompt, IReadOnlyList<ParentOption> Options);

/// <summary>
/// The catch-up a family opens from its link (P1–P4). When <see cref="Stopped"/> is true, the lesson fields are
/// null: the page shows "Messages stopped" with "Start them again" (P4).
/// </summary>
/// <param name="Answered">At least one submit on the picture check.</param>
/// <param name="ChoiceId">Their latest submitted choice, or null.</param>
/// <param name="Locked">Both submits used (D-108): the answer page shows their choice with Next only.</param>
public sealed record ParentCatchUpResponse(
    string Language,
    string ChildName,
    string TeacherName,
    bool Stopped,
    bool Answered,
    DateTimeOffset ExpiresAt,
    TextbookPage? Homework,
    ParentAudio? Audio,
    ParentTranscript? Transcript,
    ParentActivity? Activity,
    ParentCheck? Check,
    int? ChoiceId = null,
    bool Locked = false)
{
    public string Kind => "catchUp";
}

/// <summary>
/// The 410 body for an old catch-up link: enough for "This link is old", the homework page (P1) and the stop link,
/// which still works.
/// </summary>
public sealed record ParentExpiredResponse(string Language, string ChildName, string TeacherName, TextbookPage? Homework)
{
    public string Kind => "catchUp";
}

public sealed record AnswerRequest(int ChoiceId, Guid ClientRequestId);

/// <summary>
/// Which submit it was and whether both are used (D-108). Never says right or wrong: the family sees "Well done"
/// whatever the answer (D-20).
/// </summary>
public sealed record AnswerResponse(int Attempt, bool Locked);

/// <summary>
/// The child's submit on today's picture question: "&gt;", "&lt;" or "=" (T-90), or for a sum one of the three numbers it
/// showed, as text, e.g. "686" (D-116).
/// </summary>
public sealed record PictureAnswerRequest(Guid ClientRequestId, string Sign);

/// <summary>
/// What the phone needs after a submit (T-90, D-108): which try it was and whether both are used. Never right or wrong,
/// never the answer: the child may try again once whatever they chose.
/// </summary>
public sealed record PictureAnswerResponse(int Attempt, bool Locked);

/// <param name="Category">One of <see cref="ReasonCategory"/>.</param>
public sealed record ReasonRequest(string Category, Guid ClientRequestId);

public sealed record ReasonResponse(string Category);

public sealed record StopRequest(Guid ClientRequestId);

public sealed record StopResponse(bool Stopped);
