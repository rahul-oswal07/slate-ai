using SlateAi.Api.Features.WhatsAppChat;

namespace SlateAi.Api.Tests.Features.WhatsAppChat;

/// <summary>In-memory <see cref="IWhatsAppChatStore"/> for the webhook tests.</summary>
public sealed class FakeWhatsAppChatStore : IWhatsAppChatStore
{
    private readonly Lock _gate = new();
    private readonly HashSet<Guid> _events = [];
    private readonly HashSet<Guid> _pictureRequests = [];

    public Dictionary<string, ChatLesson> Lessons { get; } = [];

    public Dictionary<Guid, (byte[] Bytes, string ContentType)> Clips { get; } = [];

    /// <summary>Saved number → children, for STOP.</summary>
    public Dictionary<string, List<ChatChild>> ByPhone { get; } = [];

    public Dictionary<string, (string? First, string? Second)> Pictures { get; } = [];

    public List<(Guid ChildId, string Category, Guid RequestId)> Reasons { get; } = [];

    public List<Guid> Stopped { get; } = [];

    public Task<bool> TryRecordEventAsync(Guid eventId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_events.Add(eventId));
        }
    }

    public Task<ChatLesson?> FindLessonAsync(string token, CancellationToken cancellationToken) =>
        Task.FromResult(Lessons.GetValueOrDefault(token));

    public Task<IReadOnlyList<ChatChild>> FindByPhoneAsync(string phone, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ChatChild>>(ByPhone.GetValueOrDefault(phone) ?? []);

    public Task<(byte[] Bytes, string ContentType)?> GetClipAsync(Guid clipId, CancellationToken cancellationToken) =>
        Task.FromResult(Clips.TryGetValue(clipId, out var clip) ? clip : ((byte[], string)?)null);

    public Task<(string? First, string? Second)?> SavePictureAnswerAsync(
        string token, Guid requestId, string choice, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var (first, second) = Pictures.GetValueOrDefault(token);
            if (!_pictureRequests.Add(requestId))
            {
                return Task.FromResult<(string?, string?)?>((first, second));
            }

            if (second is not null)
            {
                return Task.FromResult<(string?, string?)?>(null);
            }

            Pictures[token] = first is null ? (choice, null) : (first, choice);
            return Task.FromResult<(string?, string?)?>(Pictures[token]);
        }
    }

    public Task SaveReasonAsync(
        ChatLesson lesson, string category, Guid requestId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            Reasons.Add((lesson.ChildId, category, requestId));
        }

        return Task.CompletedTask;
    }

    public Task SetStoppedAsync(Guid childId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            Stopped.Add(childId);
        }

        return Task.CompletedTask;
    }
}
