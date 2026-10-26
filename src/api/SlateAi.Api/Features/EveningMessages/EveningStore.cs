using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SlateAi.Api.Data;
using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Lessons;

namespace SlateAi.Api.Features.EveningMessages;

public static class TemplateStatus
{
    public const string Draft = "draft";
    public const string Approved = "approved";
}

public sealed record EveningChild(Guid Id, int RollNo, string FirstName, string HomeLanguage, string Consent, bool Stopped);

public sealed record EveningClass(Guid TeacherId, IReadOnlyList<EveningChild> Children);

public sealed record TemplateRecord(
    Guid Id,
    string Language,
    string Version,
    string Text,
    string PageFormat,
    string PageFormatNoPart,
    string Status,
    DateTimeOffset? ApprovedAt)
{
    public LanguageWording Wording => new(Text, PageFormat, PageFormatNoPart);
}

public sealed record ConfirmedLesson(Guid Id, DateOnly Date, IReadOnlyList<string> CompetencyIds);

public sealed record NewEveningMessage(
    Guid ChildId,
    Guid TemplateId,
    Guid LessonId,
    string Language,
    SlotValues Slots,
    string Token,
    DateTimeOffset SentAt,
    DateTimeOffset ExpiresAt,
    Guid? LessonAudioId = null);

/// <summary>The lesson clip she approved for this message's day and language (T-62), played after the disclosure.</summary>
public sealed record ParentLessonAudio(Guid ClipId, double Seconds, string? Transcript);

/// <summary>The class's main teacher's greeting clip, played first on the evening page.</summary>
public sealed record ParentGreeting(Guid ClipId, double Seconds);

/// <summary>A sent message as the parent view needs it. Names are allowed here: this is the final send (guardrail 1).</summary>
public sealed record ParentMessageRecord(
    DateOnly Date,
    string Language,
    string ChildName,
    string TeacherName,
    ParentGreeting? Greeting,
    string TemplateText,
    SlotValues Slots,
    DateTimeOffset SentAt,
    DateTimeOffset ExpiresAt,
    bool Stopped,
    ParentLessonAudio? LessonAudio = null);

public enum ApproveResult
{
    Approved,

    /// <summary>This clientRequestId was already applied.</summary>
    Replay,

    /// <summary>The version she saw is not the current wording (D-49): show the latest.</summary>
    Stale,
}

/// <summary>Database side of evening messages, behind an interface so endpoint tests can use a fake.</summary>
public interface IEveningStore
{
    /// <summary>The class's teacher and children with consent and stop status, or null if the class doesn't exist.</summary>
    Task<EveningClass?> GetClassAsync(Guid classId, CancellationToken cancellationToken);

    /// <summary>
    /// The class's templates, one per language in <paramref name="wording"/>. Missing languages are added as drafts;
    /// a row from an older wording version is replaced and goes back to draft (a change needs approval again, D-49).
    /// </summary>
    Task<IReadOnlyList<TemplateRecord>> GetTemplatesAsync(Guid classId, EveningWording wording, CancellationToken cancellationToken);

    /// <summary>Approves every language of the class's template at <paramref name="version"/>.</summary>
    Task<ApproveResult> ApproveAsync(
        Guid classId, Guid clientRequestId, string version, Guid teacherId, CancellationToken cancellationToken);

    /// <summary>The latest teacher-confirmed lesson on or before <paramref name="date"/>.</summary>
    Task<ConfirmedLesson?> LatestConfirmedLessonAsync(Guid classId, DateOnly date, CancellationToken cancellationToken);

    Task<IReadOnlySet<Guid>> GetSentChildIdsAsync(Guid classId, DateOnly date, CancellationToken cancellationToken);

    /// <summary>The lesson audio she approved for that day, language to row id (T-62). Only approved rows: guardrail 3.</summary>
    Task<IReadOnlyDictionary<string, Guid>> GetApprovedLessonAudioAsync(Guid classId, DateOnly date, CancellationToken cancellationToken);

    /// <summary>Stores the messages, skipping any child who already has one that date. Returns how many were added.</summary>
    Task<int> AddMessagesAsync(
        Guid classId, DateOnly date, IReadOnlyList<NewEveningMessage> messages, CancellationToken cancellationToken);

    Task<ParentMessageRecord?> FindByTokenAsync(string token, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> GetClassIdsAsync(CancellationToken cancellationToken);
}

public sealed class EveningStore(AppDbContext db, TimeProvider clock) : IEveningStore
{
    private const string ApproveEndpoint = "message-template-approve";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<EveningClass?> GetClassAsync(Guid classId, CancellationToken cancellationToken)
    {
        var teacherId = await db.Classes.Where(c => c.Id == classId).Select(c => (Guid?)c.TeacherId)
            .SingleOrDefaultAsync(cancellationToken);
        if (teacherId is null)
        {
            return null;
        }

        var children = await db.Children
            .Where(c => c.ClassId == classId)
            .OrderBy(c => c.RollNo)
            .Select(c => new EveningChild(
                c.Id, c.RollNo, c.FirstName, c.HomeLanguage, c.Consent, c.ParentStoppedAt != null))
            .ToListAsync(cancellationToken);
        return new EveningClass(teacherId.Value, children);
    }

    public async Task<IReadOnlyList<TemplateRecord>> GetTemplatesAsync(
        Guid classId, EveningWording wording, CancellationToken cancellationToken)
    {
        var rows = await db.MessageTemplates.Where(t => t.ClassId == classId).ToListAsync(cancellationToken);
        if (!NeedsUpdate(rows, wording))
        {
            return rows.Select(Record).ToList();
        }

        await using var transaction = await LockAsync($"evening-template:{classId}", cancellationToken);
        rows = await db.MessageTemplates.Where(t => t.ClassId == classId).ToListAsync(cancellationToken);
        foreach (var (language, text) in wording.Languages)
        {
            var row = rows.SingleOrDefault(r => r.Language == language);
            if (row is null)
            {
                row = new MessageTemplate
                {
                    Id = Guid.NewGuid(),
                    ClassId = classId,
                    Language = language,
                    Version = wording.Version,
                    Text = text.Text,
                    PageFormat = text.Page,
                    PageFormatNoPart = text.PageNoPart,
                    Status = TemplateStatus.Draft,
                };
                db.MessageTemplates.Add(row);
                rows.Add(row);
            }
            else if (row.Version != wording.Version)
            {
                row.Version = wording.Version;
                row.Text = text.Text;
                row.PageFormat = text.Page;
                row.PageFormatNoPart = text.PageNoPart;
                row.Status = TemplateStatus.Draft;
                row.ApprovedAt = null;
                row.ApprovedByTeacherId = null;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return rows.Select(Record).ToList();
    }

    public async Task<ApproveResult> ApproveAsync(
        Guid classId, Guid clientRequestId, string version, Guid teacherId, CancellationToken cancellationToken)
    {
        await using var transaction = await LockAsync($"evening-template:{classId}", cancellationToken);
        if (await db.ProcessedRequests.AnyAsync(r => r.ClientRequestId == clientRequestId, cancellationToken))
        {
            return ApproveResult.Replay;
        }

        var rows = await db.MessageTemplates.Where(t => t.ClassId == classId).ToListAsync(cancellationToken);
        if (rows.Count == 0 || rows.Any(r => r.Version != version))
        {
            return ApproveResult.Stale;
        }

        var now = clock.GetUtcNow();
        foreach (var row in rows.Where(r => r.Status != TemplateStatus.Approved))
        {
            row.Status = TemplateStatus.Approved;
            row.ApprovedAt = now;
            row.ApprovedByTeacherId = teacherId;
        }

        db.ProcessedRequests.Add(new ProcessedRequest
        {
            ClientRequestId = clientRequestId,
            Endpoint = ApproveEndpoint,
            CreatedAt = now,
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ApproveResult.Approved;
    }

    public Task<ConfirmedLesson?> LatestConfirmedLessonAsync(Guid classId, DateOnly date, CancellationToken cancellationToken) =>
        db.Lessons
            .Where(l => l.ClassId == classId && l.Date <= date && l.Status == LessonValues.Confirmed)
            .OrderByDescending(l => l.Date)
            .Select(l => new ConfirmedLesson(l.Id, l.Date, l.CompetencyIds))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlySet<Guid>> GetSentChildIdsAsync(Guid classId, DateOnly date, CancellationToken cancellationToken) =>
        (await db.EveningMessages
            .Where(m => m.ClassId == classId && m.Date == date)
            .Select(m => m.ChildId)
            .ToListAsync(cancellationToken))
        .ToHashSet();

    public async Task<IReadOnlyDictionary<string, Guid>> GetApprovedLessonAudioAsync(
        Guid classId, DateOnly date, CancellationToken cancellationToken) =>
        await db.LessonAudios
            .Where(l => l.ClassId == classId && l.Date == date && l.Status == "approved" && l.ApprovedClipId != null && l.ApprovedClipId == l.ClipId)
            .ToDictionaryAsync(l => l.Language, l => l.Id, cancellationToken);

    public async Task<int> AddMessagesAsync(
        Guid classId, DateOnly date, IReadOnlyList<NewEveningMessage> messages, CancellationToken cancellationToken)
    {
        // The 6 pm job and a demo send can overlap; the lock plus the (ChildId, Date) index keep it to one each.
        await using var transaction = await LockAsync($"evening:{classId}:{date:yyyy-MM-dd}", cancellationToken);
        var sent = await GetSentChildIdsAsync(classId, date, cancellationToken);
        var added = 0;
        foreach (var message in messages.Where(m => !sent.Contains(m.ChildId)))
        {
            db.EveningMessages.Add(new EveningMessage
            {
                Id = Guid.NewGuid(),
                ClassId = classId,
                ChildId = message.ChildId,
                Date = date,
                TemplateId = message.TemplateId,
                LessonId = message.LessonId,
                LessonAudioId = message.LessonAudioId,
                Language = message.Language,
                SlotValues = JsonSerializer.Serialize(message.Slots, Json),
                Token = message.Token,
                SentAt = message.SentAt.ToUniversalTime(),
                ExpiresAt = message.ExpiresAt.ToUniversalTime(),
            });
            added++;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return added;
    }

    public async Task<ParentMessageRecord?> FindByTokenAsync(string token, CancellationToken cancellationToken)
    {
        var row = await (
                from m in db.EveningMessages
                where m.Token == token
                join c in db.Children on m.ChildId equals c.Id
                join t in db.MessageTemplates on m.TemplateId equals t.Id
                join k in db.Classes on m.ClassId equals k.Id
                join teacher in db.Teachers on k.TeacherId equals teacher.Id
                select new
                {
                    m.Date, m.Language, c.FirstName, teacher.DisplayName, t.Text,
                    m.SlotValues, m.SentAt, m.ExpiresAt, Stopped = c.ParentStoppedAt != null,
                    Greeting = db.AudioClips
                        .Where(a => a.Kind == "greeting" && a.TeacherId == teacher.Id)
                        .Select(a => new ParentGreeting(a.Id, a.Seconds))
                        .FirstOrDefault(),
                    LessonAudio = db.LessonAudios
                        .Where(l => l.Id == m.LessonAudioId && l.Status == "approved" && l.ApprovedClipId == l.ClipId)
                        .Join(db.AudioClips, l => l.ApprovedClipId, a => a.Id, (l, a) => new ParentLessonAudio(a.Id, a.Seconds, a.Transcript))
                        .FirstOrDefault(),
                })
            .SingleOrDefaultAsync(cancellationToken);
        return row is null
            ? null
            : new ParentMessageRecord(
                row.Date, row.Language, row.FirstName, row.DisplayName, row.Greeting, row.Text,
                JsonSerializer.Deserialize<SlotValues>(row.SlotValues, Json)!, row.SentAt, row.ExpiresAt, row.Stopped, row.LessonAudio);
    }

    public async Task<IReadOnlyList<Guid>> GetClassIdsAsync(CancellationToken cancellationToken) =>
        await db.Classes.Select(c => c.Id).ToListAsync(cancellationToken);

    private static bool NeedsUpdate(List<MessageTemplate> rows, EveningWording wording) =>
        wording.Languages.Keys.Any(language =>
            rows.SingleOrDefault(r => r.Language == language) is not { } row || row.Version != wording.Version);

    private static TemplateRecord Record(MessageTemplate t) =>
        new(t.Id, t.Language, t.Version, t.Text, t.PageFormat, t.PageFormatNoPart, t.Status, t.ApprovedAt);

    private async Task<IDbContextTransaction> LockAsync(string key, CancellationToken cancellationToken)
    {
        var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", cancellationToken);
        return transaction;
    }
}
