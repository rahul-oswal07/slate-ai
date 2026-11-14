using SlateAi.Api.Features.EveningMessages;

namespace SlateAi.Api.Tests.Features.EveningMessages;

/// <summary>In-memory <see cref="IEveningStore"/> for endpoint tests.</summary>
public sealed class FakeEveningStore(Guid classId, Guid teacherId) : IEveningStore
{
    private readonly Lock _gate = new();
    private readonly HashSet<Guid> _processed = [];

    public List<EveningChild> Children { get; } = [];

    public List<TemplateRecord> Templates { get; } = [];

    public List<ConfirmedLesson> Lessons { get; } = [];

    public List<(DateOnly Date, NewEveningMessage Message)> Messages { get; } = [];

    public string TeacherName { get; set; } = "Teacher";

    public ParentGreeting? Greeting { get; set; }

    /// <summary>The approved lesson audio per language for a day (T-62); empty by default: the plain message.</summary>
    public Func<DateOnly, IReadOnlyDictionary<string, Guid>> ApprovedLessonAudio { get; set; } = _ => new Dictionary<string, Guid>();

    /// <summary>The day's today's message when approved at its current version (T-88); null by default: the plain message.</summary>
    public Func<DateOnly, (Guid Id, IReadOnlySet<string> Languages)?> ApprovedTodaysMessage { get; set; } = _ => null;

    /// <summary>What the parent link shows as today's message for a message that carries one (T-88).</summary>
    public ParentTodaysMessage? TodaysMessage { get; set; }

    /// <summary>What the parent link shows as lesson audio (T-62).</summary>
    public ParentLessonAudio? LessonAudio { get; set; }

    public int Approvals { get; private set; }

    public Task<EveningClass?> GetClassAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(id == classId ? new EveningClass(teacherId, Children.ToList()) : null);

    public Task<IReadOnlyList<TemplateRecord>> GetTemplatesAsync(
        Guid id, EveningWording wording, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            foreach (var (language, text) in wording.Languages)
            {
                var index = Templates.FindIndex(t => t.Language == language);
                if (index < 0 || Templates[index].Version != wording.Version)
                {
                    var record = new TemplateRecord(
                        index < 0 ? Guid.NewGuid() : Templates[index].Id, language, wording.Version, text.Text, text.Page,
                        text.PageNoPart, TemplateStatus.Draft, null);
                    if (index < 0)
                    {
                        Templates.Add(record);
                    }
                    else
                    {
                        Templates[index] = record;
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<TemplateRecord>>(Templates.ToList());
        }
    }

    public Task<ApproveResult> ApproveAsync(
        Guid id, Guid clientRequestId, string version, Guid teacher, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (!_processed.Add(clientRequestId))
            {
                return Task.FromResult(ApproveResult.Replay);
            }

            if (Templates.Count == 0 || Templates.Any(t => t.Version != version))
            {
                return Task.FromResult(ApproveResult.Stale);
            }

            Approvals++;
            for (var i = 0; i < Templates.Count; i++)
            {
                Templates[i] = Templates[i] with
                {
                    Status = TemplateStatus.Approved,
                    ApprovedAt = Templates[i].ApprovedAt ?? DateTimeOffset.UnixEpoch,
                };
            }

            return Task.FromResult(ApproveResult.Approved);
        }
    }

    public Task<ConfirmedLesson?> LatestConfirmedLessonAsync(Guid id, DateOnly date, CancellationToken cancellationToken) =>
        Task.FromResult(Lessons.Where(l => l.Date <= date).OrderByDescending(l => l.Date).FirstOrDefault());

    public Task<IReadOnlySet<Guid>> GetSentChildIdsAsync(Guid id, DateOnly date, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlySet<Guid>>(
                Messages.Where(m => m.Date == date).Select(m => m.Message.ChildId).ToHashSet());
        }
    }

    public Task<int> AddMessagesAsync(
        Guid id, DateOnly date, IReadOnlyList<NewEveningMessage> messages, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var sent = Messages.Where(m => m.Date == date).Select(m => m.Message.ChildId).ToHashSet();
            var added = messages.Where(m => sent.Add(m.ChildId)).ToList();
            Messages.AddRange(added.Select(m => (date, m)));
            return Task.FromResult(added.Count);
        }
    }

    public Task<ParentMessageRecord?> FindByTokenAsync(string token, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var found = Messages.Where(m => m.Message.Token == token).Select(m => ((DateOnly, NewEveningMessage)?)m).FirstOrDefault();
            if (found is not var (date, message))
            {
                return Task.FromResult<ParentMessageRecord?>(null);
            }

            var child = Children.Single(c => c.Id == message.ChildId);
            var template = Templates.Single(t => t.Id == message.TemplateId);
            return Task.FromResult<ParentMessageRecord?>(new ParentMessageRecord(
                date, message.Language, child.FirstName, TeacherName, Greeting, template.Text, message.Slots,
                message.SentAt, message.ExpiresAt, child.Stopped, message.LessonAudioId is null ? null : LessonAudio,
                message.TodaysMessageId is null || TodaysMessage is null
                    ? null
                    : TodaysMessage with
                    {
                        Picture = TodaysMessage.Picture is { } p
                            ? p with
                            {
                                FirstAnswer = PictureTaps.GetValueOrDefault(message.Token).First,
                                SecondAnswer = PictureTaps.GetValueOrDefault(message.Token).Second,
                                Done = SlateAi.Api.Features.TodaysMessage.TodaysLevel.Done(
                                    PictureTaps.GetValueOrDefault(message.Token).First, PictureTaps.GetValueOrDefault(message.Token).Second, PictureAnswer),
                            }
                            : null,
                    },
                message.TodaysMessageId is null || TodaysMessage?.Picture is null ? null : PictureAnswer));
        }
    }

    /// <summary>The picture answer for messages that carry today's message (T-90); taps recorded per token.</summary>
    public string PictureAnswer { get; set; } = ">";

    public Dictionary<string, (string? First, string? Second)> PictureTaps { get; } = [];

    private readonly HashSet<Guid> _pictureRequests = [];

    public Task<(string? First, string? Second)?> SavePictureAnswerAsync(
        string token, Guid clientRequestId, string sign, string answer, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (!Messages.Any(m => m.Message.Token == token))
            {
                return Task.FromResult<(string?, string?)?>(null);
            }

            var taps = PictureTaps.GetValueOrDefault(token);
            if (!_pictureRequests.Add(clientRequestId))
            {
                return Task.FromResult<(string?, string?)?>(taps);
            }

            if (taps.First is null)
            {
                taps = (sign, null);
            }
            else if (taps.First != answer && taps.Second is null)
            {
                taps = (taps.First, sign);
            }
            else
            {
                return Task.FromResult<(string?, string?)?>(null);
            }

            PictureTaps[token] = taps;
            return Task.FromResult<(string?, string?)?>(taps);
        }
    }

    public Task<(Guid Id, IReadOnlySet<string> Languages)?> GetApprovedTodaysMessageAsync(
        Guid id, DateOnly date, CancellationToken cancellationToken) =>
        Task.FromResult(ApprovedTodaysMessage(date));

    public Task<IReadOnlyDictionary<string, Guid>> GetApprovedLessonAudioAsync(
        Guid id, DateOnly date, CancellationToken cancellationToken) =>
        Task.FromResult(ApprovedLessonAudio(date));

    public Task<IReadOnlyList<Guid>> GetClassIdsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Guid>>([classId]);
}
