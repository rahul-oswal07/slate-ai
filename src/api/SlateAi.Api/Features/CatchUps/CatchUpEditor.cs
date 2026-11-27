using Microsoft.Extensions.Options;
using SlateAi.Api.Curriculum;
using SlateAi.Api.Features.CatchUps.Verify;

namespace SlateAi.Api.Features.CatchUps;

/// <summary>
/// Her changes to a catch-up's sections (T-103, D-129), as on today's message (D-92): her words are checked by code and
/// saved as a new version that clears her approval (guardrail 3). An edited script is voiced again in the background.
/// </summary>
public sealed class CatchUpEditor(
    ICatchUpStore store, ICurriculumProvider curricula, IOptions<VerifierOptions> options, CatchUpQueue queue)
{
    /// <summary>
    /// Her words for one section. On a problem nothing is saved. Applied: a new version, ready again (needs review if a
    /// check still fails), or generating while her script is voiced.
    /// </summary>
    public async Task<(PackageChange Change, CatchUpEditProblem? Problem)> EditAsync(
        PackageView view, Guid clientRequestId, int scriptVersion, PackageEdit edit, CancellationToken cancellationToken)
    {
        var curriculum = await curricula.ForClassAsync(view.ClassId, cancellationToken);
        var names = await store.GetNamesAsync(view.ClassId, cancellationToken);
        var (problem, passed) = CatchUpEditCheck.Check(view, edit, curriculum, names, options.Value);
        if (problem is not null)
        {
            return (PackageChange.Conflict, problem);
        }

        var revision = new PackageRevision(
            edit.Kind,
            edit.Text!.Trim(),
            edit.Kind == CatchUpSectionKind.Activity ? edit.ObjectOption?.Trim() ?? "" : null,
            Review: CatchUpEditCheck.Review(view.Review, edit.Kind, passed));
        var change = await store.ReviseAsync(view.Id, clientRequestId, scriptVersion, revision, cancellationToken);
        if (change == PackageChange.Applied && edit.Kind == CatchUpSectionKind.Concept)
        {
            // Not queued (full): it stays generating, reads as failed after 5 minutes, and Try again writes it anew (T-136).
            queue.TryEnqueue(view.Id);
        }

        return (change, null);
    }

    /// <summary>Removes or puts back the video or her note: a new version, her approval cleared.</summary>
    public Task<PackageChange> SetRemovedAsync(
        Guid packageId, Guid clientRequestId, string kind, bool removed, CancellationToken cancellationToken) =>
        store.ReviseAsync(packageId, clientRequestId, null, new PackageRevision(kind, Removed: removed), cancellationToken);
}
