using SlateAi.Api.Data.Entities;
using SlateAi.Api.Features.TextbookImport;

namespace SlateAi.Api.Tests.Features.TextbookImport;

/// <summary>In-memory <see cref="ITextbookImportStore"/> with the same rules: replayed uploads, claim once, resume, discard.</summary>
public sealed class FakeTextbookImportStore : ITextbookImportStore
{
    private readonly Lock _lock = new();

    public Dictionary<Guid, CurriculumImport> Imports { get; } = [];

    public Dictionary<Guid, List<byte[]>> Pages { get; } = [];

    public Dictionary<Guid, Dictionary<string, string>> Steps { get; } = [];

    /// <summary>Drafts written, by import.</summary>
    public Dictionary<Guid, (Guid CurriculumId, DraftCurriculum Draft)> Drafts { get; } = [];

    /// <summary>Drafts deleted by a discard.</summary>
    public List<Guid> DiscardedDrafts { get; } = [];

    public Task<(CurriculumImport Import, bool Created)> CreateAsync(
        Guid clientRequestId, ImportBook book, IReadOnlyList<byte[]> pages, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (Imports.Values.FirstOrDefault(i => i.ClientRequestId == clientRequestId) is { } existing)
            {
                return Task.FromResult((Copy(existing), false));
            }

            var import = Add(book, pages, now, clientRequestId);
            return Task.FromResult((Copy(import), true));
        }
    }

    /// <summary>Adds a queued import, as an upload would.</summary>
    public CurriculumImport Add(ImportBook book, IReadOnlyList<byte[]> pages, DateTimeOffset now, Guid? clientRequestId = null)
    {
        lock (_lock)
        {
            var import = new CurriculumImport
            {
                Id = Guid.NewGuid(),
                ClientRequestId = clientRequestId ?? Guid.NewGuid(),
                Title = book.Title,
                Grade = book.Grade,
                Subject = book.Subject,
                Medium = book.Medium,
                Book = book.Book,
                Part = book.Part,
                PageCount = pages.Count,
                Status = CurriculumImportStatuses.Queued,
                CreatedAt = now,
                UpdatedAt = now,
            };
            Imports[import.Id] = import;
            Pages[import.Id] = [.. pages];
            Steps[import.Id] = [];
            return import;
        }
    }

    public Task<CurriculumImport?> FindByRequestAsync(Guid clientRequestId, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult(Imports.Values.FirstOrDefault(i => i.ClientRequestId == clientRequestId) is { } i ? Copy(i) : null);
        }
    }

    public Task<IReadOnlyList<CurriculumImport>> ListAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<CurriculumImport>>(Imports.Values.OrderByDescending(i => i.CreatedAt).Select(Copy).ToList());
        }
    }

    public Task<CurriculumImport?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult(Imports.TryGetValue(id, out var i) ? Copy(i) : null);
        }
    }

    public Task<(int Lessons, int Flagged)?> CountDraftAsync(Guid id, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult<(int, int)?>(Drafts.TryGetValue(id, out var d)
                ? (d.Draft.Lessons.Count, d.Draft.Lessons.Count(l => l.Flags.Count > 0))
                : null);
        }
    }

    public Task<CurriculumImport?> TryStartAsync(Guid id, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (!Imports.TryGetValue(id, out var import) || import.Status != CurriculumImportStatuses.Queued)
            {
                return Task.FromResult<CurriculumImport?>(null);
            }

            import.Status = CurriculumImportStatuses.Running;
            import.Error = null;
            import.UpdatedAt = now;
            return Task.FromResult<CurriculumImport?>(Copy(import));
        }
    }

    public Task<IReadOnlyList<int>> GetPageSizesAsync(Guid id, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<int>>(Pages[id].Select(p => p.Length).ToList());
        }
    }

    public Task<IReadOnlyList<byte[]>> GetPagesAsync(Guid id, IReadOnlyList<int> numbers, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<byte[]>>(numbers.Select(n => Pages[id][n - 1]).ToList());
        }
    }

    public Task<IReadOnlyDictionary<string, string>> GetStepsAsync(Guid id, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>(Steps[id]));
        }
    }

    public Task SaveStepAsync(Guid id, string key, string result, int stepsDone, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            Steps[id].Add(key, result); // a step is saved once, like the primary key
            Imports[id].StepsDone = stepsDone;
            Imports[id].UpdatedAt = now;
            return Task.CompletedTask;
        }
    }

    public Task SetProgressAsync(Guid id, string stage, int stepsDone, int stepsTotal, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            var import = Imports[id];
            (import.Stage, import.StepsDone, import.StepsTotal, import.UpdatedAt) = (stage, stepsDone, stepsTotal, now);
            return Task.CompletedTask;
        }
    }

    public Task FailAsync(Guid id, string error, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (Imports.TryGetValue(id, out var import))
            {
                (import.Status, import.Error, import.UpdatedAt) = (CurriculumImportStatuses.Failed, error, now);
            }

            return Task.CompletedTask;
        }
    }

    public Task<Guid> SaveDraftAsync(
        Guid id, DraftCurriculum draft, IReadOnlyList<string> notes, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            var curriculumId = Guid.NewGuid();
            Drafts[id] = (curriculumId, draft);
            var import = Imports[id];
            (import.Status, import.Stage, import.Error, import.CurriculumId, import.Notes, import.StepsDone, import.UpdatedAt) =
                (CurriculumImportStatuses.Ready, null, null, curriculumId, [.. notes], import.StepsTotal, now);
            Pages[id].Clear();
            return Task.FromResult(curriculumId);
        }
    }

    public Task<ImportRetry> RetryAsync(Guid id, DateTimeOffset staleBefore, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (!Imports.TryGetValue(id, out var import))
            {
                return Task.FromResult(ImportRetry.NotFound);
            }

            if (import.Status == CurriculumImportStatuses.Failed
                || (import.Status == CurriculumImportStatuses.Running && import.UpdatedAt < staleBefore))
            {
                (import.Status, import.Error, import.UpdatedAt) = (CurriculumImportStatuses.Queued, null, now);
                return Task.FromResult(ImportRetry.Queued);
            }

            return Task.FromResult(ImportRetry.Unchanged);
        }
    }

    public Task<bool> DiscardAsync(Guid id, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (!Imports.Remove(id))
            {
                return Task.FromResult(false);
            }

            Pages.Remove(id);
            Steps.Remove(id);
            if (Drafts.Remove(id, out var draft))
            {
                DiscardedDrafts.Add(draft.CurriculumId);
            }

            return Task.FromResult(true);
        }
    }

    private static CurriculumImport Copy(CurriculumImport i) => new()
    {
        Id = i.Id, ClientRequestId = i.ClientRequestId, Title = i.Title, Grade = i.Grade, Subject = i.Subject, Medium = i.Medium,
        Book = i.Book, Part = i.Part, PageCount = i.PageCount, Status = i.Status, Stage = i.Stage, StepsDone = i.StepsDone,
        StepsTotal = i.StepsTotal, Error = i.Error, Notes = i.Notes, CurriculumId = i.CurriculumId, CreatedAt = i.CreatedAt,
        UpdatedAt = i.UpdatedAt,
    };
}
