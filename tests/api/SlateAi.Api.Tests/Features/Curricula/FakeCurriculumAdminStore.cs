using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.Curricula;

namespace SlateAi.Api.Tests.Features.Curricula;

/// <summary>In-memory <see cref="ICurriculumAdminStore"/> applying the same <see cref="DraftRules"/> as the real one.</summary>
public sealed class FakeCurriculumAdminStore : ICurriculumAdminStore
{
    private readonly Lock _lock = new();

    public Dictionary<Guid, CurriculumDraft> Drafts { get; } = [];

    public List<AdminClassChoice> Classes { get; } = [];

    /// <summary>Lesson IDs each class has used.</summary>
    public Dictionary<Guid, List<string>> Used { get; } = [];

    public Task<IReadOnlyList<CurriculumDraft>> ListAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<CurriculumDraft>>(Drafts.Values.Select(WithClasses).ToList());
        }
    }

    public Task<CurriculumDraft?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult(Drafts.TryGetValue(id, out var d) ? WithClasses(d) : null);
        }
    }

    public Task<IReadOnlyList<AdminClassChoice>> ListClassesAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<AdminClassChoice>>([.. Classes]);
        }
    }

    public Task<DraftChangeResult> EditLessonAsync(
        Guid id, string competencyId, EditDraftLessonRequest edit, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (!Drafts.TryGetValue(id, out var draft) || draft.Lessons.FirstOrDefault(l => l.Competency.Id == competencyId) is not { } lesson)
            {
                return Task.FromResult(new DraftChangeResult(DraftChange.NotFound));
            }

            if (draft.Meta.Status != CurriculumStatuses.Draft)
            {
                return Task.FromResult(new DraftChangeResult(DraftChange.NotDraft));
            }

            var (edited, errors) = DraftRules.Apply(draft, lesson, edit);
            if (edited is null)
            {
                return Task.FromResult(new DraftChangeResult(DraftChange.Invalid, errors));
            }

            Drafts[id] = draft with { Lessons = draft.Lessons.Select(l => l.Competency.Id == competencyId ? edited : l).ToList() };
            return Task.FromResult(new DraftChangeResult(DraftChange.Applied));
        }
    }

    public Task<DraftChange> RemoveLessonAsync(
        Guid id, string competencyId, Guid clientRequestId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (!Drafts.TryGetValue(id, out var draft) || draft.Lessons.All(l => l.Competency.Id != competencyId))
            {
                return Task.FromResult(DraftChange.NotFound);
            }

            if (draft.Meta.Status != CurriculumStatuses.Draft)
            {
                return Task.FromResult(DraftChange.NotDraft);
            }

            Drafts[id] = draft with
            {
                Lessons = draft.Lessons.Where(l => l.Competency.Id != competencyId)
                    .Select(l => l with { Competency = l.Competency with { Prerequisites = l.Competency.Prerequisites.Where(p => p != competencyId).ToList() } })
                    .ToList(),
            };
            return Task.FromResult(DraftChange.Applied);
        }
    }

    public Task<DraftChangeResult> PublishAsync(Guid id, Guid clientRequestId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (!Drafts.TryGetValue(id, out var draft))
            {
                return Task.FromResult(new DraftChangeResult(DraftChange.NotFound));
            }

            if (draft.Meta.Status != CurriculumStatuses.Draft)
            {
                return Task.FromResult(new DraftChangeResult(DraftChange.NotDraft));
            }

            if (DraftRules.PublishProblems(draft) is { Count: > 0 } problems)
            {
                return Task.FromResult(new DraftChangeResult(DraftChange.Invalid, Problems: problems));
            }

            draft.Meta.Status = CurriculumStatuses.Published;
            draft.Meta.PublishedAt = now;
            return Task.FromResult(new DraftChangeResult(DraftChange.Applied));
        }
    }

    public Task<(ClassAssign Result, IReadOnlyList<string> Missing)> AssignAsync(
        Guid classId, Guid? curriculumId, IReadOnlySet<string> builtInIds, Guid clientRequestId, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            var index = Classes.FindIndex(c => c.Id == classId);
            if (index < 0)
            {
                return Task.FromResult<(ClassAssign, IReadOnlyList<string>)>((ClassAssign.ClassNotFound, []));
            }

            IReadOnlySet<string> target = builtInIds;
            if (curriculumId is { } id)
            {
                if (!Drafts.TryGetValue(id, out var draft) || draft.Meta.Status != CurriculumStatuses.Published)
                {
                    return Task.FromResult<(ClassAssign, IReadOnlyList<string>)>((ClassAssign.CurriculumNotPublished, []));
                }

                target = draft.Lessons.Select(l => l.Competency.Id).ToHashSet();
            }

            if (DraftRules.Missing(Used.GetValueOrDefault(classId) ?? [], target) is { Count: > 0 } missing)
            {
                return Task.FromResult<(ClassAssign, IReadOnlyList<string>)>((ClassAssign.WouldLoseLessons, missing));
            }

            Classes[index] = Classes[index] with { CurriculumId = curriculumId };
            return Task.FromResult<(ClassAssign, IReadOnlyList<string>)>((ClassAssign.Applied, []));
        }
    }

    private CurriculumDraft WithClasses(CurriculumDraft draft) =>
        draft with { ClassIds = Classes.Where(c => c.CurriculumId == draft.Meta.Id).Select(c => c.Id).ToList() };
}
