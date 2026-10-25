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
                message.SentAt, message.ExpiresAt, child.Stopped));
        }
    }

    public Task<IReadOnlyList<Guid>> GetClassIdsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Guid>>([classId]);
}
