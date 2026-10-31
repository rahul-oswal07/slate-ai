using Microsoft.EntityFrameworkCore;
using SlateAi.Api.Data;
using SlateAi.Api.Features.Audio;

namespace SlateAi.Api.Features.Admin;

/// <summary>One teacher's recorded greeting, without its bytes.</summary>
public sealed record AdminGreeting(Guid ClipId, string TeacherName, double Seconds, DateTimeOffset RecordedAt);

/// <summary>A clip's audio.</summary>
public sealed record AdminClip(byte[] Bytes, string ContentType);

/// <summary>A family one lesson audio went to with its evening message.</summary>
public sealed record AdminFamily(string ChildName, int RollNo, string Language, DateTimeOffset SentAt);

/// <summary>One class's lesson audio in one language on one day (T-62), as stored; lesson names are added by code.</summary>
/// <param name="ClipId">The approved clip, else the one waiting for approval; null while it is made or after a failure.</param>
public sealed record AdminLessonAudioRow(
    Guid Id,
    string TeacherName,
    int Grade,
    string Language,
    IReadOnlyList<string> CompetencyIds,
    string Status,
    string? Verdict,
    string? ApprovedBy,
    DateTimeOffset? ApprovedAt,
    Guid? ClipId,
    double? Seconds,
    string? Transcript,
    IReadOnlyList<AdminFamily> Families);

/// <summary>Database side of the admin pages, kept behind an interface so endpoint tests can use a fake.</summary>
public interface IAdminStore
{
    /// <summary>Every teacher's greeting, newest first.</summary>
    Task<IReadOnlyList<AdminGreeting>> ListGreetingsAsync(CancellationToken cancellationToken);

    /// <summary>The clip's audio if it is of <paramref name="kind"/> (<see cref="AudioKind"/>); null otherwise.</summary>
    Task<AdminClip?> GetClipAsync(Guid clipId, string kind, CancellationToken cancellationToken);

    /// <summary>Every class's lesson audio on <paramref name="date"/>, sent or not, by class then language.</summary>
    Task<IReadOnlyList<AdminLessonAudioRow>> GetLessonAudioDayAsync(DateOnly date, CancellationToken cancellationToken);

    /// <summary>Evening messages sent on <paramref name="date"/> that carried a lesson audio.</summary>
    Task<int> CountLessonAudioSentAsync(DateOnly date, CancellationToken cancellationToken);
}

public sealed class AdminStore(AppDbContext db) : IAdminStore
{
    public async Task<IReadOnlyList<AdminGreeting>> ListGreetingsAsync(CancellationToken cancellationToken)
    {
        var rows = await (
                from clip in db.AudioClips
                where clip.Kind == AudioKind.Greeting
                join teacher in db.Teachers on clip.TeacherId equals teacher.Id
                select new { clip.Id, teacher.DisplayName, clip.Seconds, clip.CreatedAt })
            .ToListAsync(cancellationToken);
        return rows
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new AdminGreeting(r.Id, r.DisplayName, Math.Round(r.Seconds, 1), r.CreatedAt))
            .ToList();
    }

    public async Task<AdminClip?> GetClipAsync(Guid clipId, string kind, CancellationToken cancellationToken) =>
        await db.AudioClips
            .Where(c => c.Id == clipId && c.Kind == kind)
            .Select(c => new AdminClip(c.Bytes, c.ContentType))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<AdminLessonAudioRow>> GetLessonAudioDayAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var audios = await (
                from audio in db.LessonAudios
                where audio.Date == date
                join cls in db.Classes on audio.ClassId equals cls.Id
                join teacher in db.Teachers on cls.TeacherId equals teacher.Id
                select new
                {
                    audio.Id,
                    audio.ClassId,
                    TeacherName = teacher.DisplayName,
                    cls.Grade,
                    audio.Language,
                    audio.Status,
                    audio.Verdict,
                    audio.ApprovedByTeacherId,
                    audio.ApprovedAt,
                    ClipId = audio.ApprovedClipId ?? audio.ClipId,
                })
            .ToListAsync(cancellationToken);
        if (audios.Count == 0)
        {
            return [];
        }

        var classIds = audios.Select(a => a.ClassId).Distinct().ToList();
        var clipIds = audios.Where(a => a.ClipId is not null).Select(a => a.ClipId!.Value).ToList();
        var approverIds = audios.Where(a => a.ApprovedByTeacherId is not null).Select(a => a.ApprovedByTeacherId!.Value).ToList();
        var audioIds = audios.Select(a => a.Id).ToList();

        var lessons = await db.Lessons
            .Where(l => l.Date == date && classIds.Contains(l.ClassId))
            .ToDictionaryAsync(l => l.ClassId, l => l.CompetencyIds, cancellationToken);
        var clips = await db.AudioClips
            .Where(c => clipIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Seconds, c.Transcript })
            .ToDictionaryAsync(c => c.Id, cancellationToken);
        var approvers = await db.Teachers
            .Where(t => approverIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.DisplayName, cancellationToken);
        var families = (await (
                    from message in db.EveningMessages
                    where message.Date == date && message.LessonAudioId != null && audioIds.Contains(message.LessonAudioId.Value)
                    join child in db.Children on message.ChildId equals child.Id
                    select new { AudioId = message.LessonAudioId!.Value, child.FirstName, child.RollNo, message.Language, message.SentAt })
                .ToListAsync(cancellationToken))
            .ToLookup(f => f.AudioId);

        return audios
            .OrderBy(a => a.TeacherName).ThenBy(a => a.Grade).ThenBy(a => a.Language)
            .Select(a =>
            {
                var clip = a.ClipId is { } id ? clips.GetValueOrDefault(id) : null;
                return new AdminLessonAudioRow(
                    a.Id,
                    a.TeacherName,
                    a.Grade,
                    a.Language,
                    lessons.GetValueOrDefault(a.ClassId) ?? [],
                    a.Status,
                    a.Verdict,
                    a.ApprovedByTeacherId is { } by ? approvers.GetValueOrDefault(by) : null,
                    a.ApprovedAt,
                    clip?.Id,
                    clip is null ? null : Math.Round(clip.Seconds, 1),
                    clip?.Transcript,
                    families[a.Id]
                        .OrderBy(f => f.RollNo)
                        .Select(f => new AdminFamily(f.FirstName, f.RollNo, f.Language, f.SentAt))
                        .ToList());
            })
            .ToList();
    }

    public Task<int> CountLessonAudioSentAsync(DateOnly date, CancellationToken cancellationToken) =>
        db.EveningMessages.CountAsync(m => m.Date == date && m.LessonAudioId != null, cancellationToken);
}
