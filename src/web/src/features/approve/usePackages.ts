import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api, ApiError } from '../../api/client'
import type { CatchUpPackage, CatchUpWeek } from './types'

export const packagesKey = (classId: string) => ['packages', classId] as const

/** This week's catch-ups (T3). Online only (D-70); checks again every 3 s while any is being written. */
export function usePackages(classId: string) {
  return useQuery({
    queryKey: packagesKey(classId),
    queryFn: () => api<CatchUpWeek>(`/api/classes/${classId}/packages`),
    enabled: classId !== '',
    retry: false,
    refetchInterval: (query) => (query.state.data?.packages.some((p) => p.status === 'generating') ? 3000 : false),
  })
}

/** Waiting for her: ready or needs review (the nav badge; generating and failed are not counted, T3). */
export const waitingCount = (week: CatchUpWeek | undefined) =>
  week?.packages.filter((p) => p.status === 'ready' || p.status === 'needsReview').length ?? 0

/** Makes the week's catch-ups (A2 → A3 → verifier → audio in the background). */
export function useGenerate(classId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () =>
      api<CatchUpWeek>(`/api/classes/${classId}/catchups/generate`, {
        method: 'POST',
        body: { clientRequestId: crypto.randomUUID() },
      }),
    onSuccess: (week) => queryClient.setQueryData(packagesKey(classId), week),
  })
}

function replace(queryClient: ReturnType<typeof useQueryClient>, classId: string, updated: CatchUpPackage) {
  queryClient.setQueryData<CatchUpWeek>(packagesKey(classId), (week) =>
    week && { ...week, packages: week.packages.map((p) => (p.id === updated.id ? updated : p)) },
  )
}

/** Approve one catch-up at the version she ticked; a 409 refreshes to the latest (D-68). */
export function useApprove(classId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    // send: "Approve and send now" (T-117), it goes to the families at once; false: the 6 pm job sends it.
    mutationFn: ({ id, scriptVersion, send }: { id: string; scriptVersion: number; send: boolean }) =>
      api<CatchUpPackage>(`/api/packages/${id}/approve`, {
        method: 'POST',
        body: { clientRequestId: crypto.randomUUID(), scriptVersion, send },
      }),
    onSuccess: (updated) => replace(queryClient, classId, updated),
    onError: () => void queryClient.invalidateQueries({ queryKey: packagesKey(classId) }),
  })
}

/**
 * Her words for one section (T-103). The API answers with the latest catch-up, also on a 409 (an older version, or it
 * changed meanwhile), so the screen shows what is current; a 400 says what to fix and changes nothing.
 */
export function useEditPackageSection(classId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({
      id,
      kind,
      scriptVersion,
      text,
      objectOption,
    }: {
      id: string
      kind: 'concept' | 'activity' | 'note'
      scriptVersion: number
      text: string
      objectOption?: string
    }) =>
      api<CatchUpPackage>(`/api/packages/${id}/sections/${kind}`, {
        method: 'PUT',
        body: { clientRequestId: crypto.randomUUID(), scriptVersion, text, objectOption },
      }),
    onSuccess: (updated) => replace(queryClient, classId, updated),
    onError: (error) => latestOn409(queryClient, classId, error),
  })
}

/** Removes or puts back the video or her note (T-103): a new version, her tick cleared. */
export function useSetPackageRemoved(classId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, kind, removed }: { id: string; kind: 'video' | 'note'; removed: boolean }) =>
      api<CatchUpPackage>(`/api/packages/${id}/sections/${kind}`, {
        method: 'PATCH',
        body: { clientRequestId: crypto.randomUUID(), removed },
      }),
    onSuccess: (updated) => replace(queryClient, classId, updated),
    onError: (error) => latestOn409(queryClient, classId, error),
  })
}

function latestOn409(queryClient: ReturnType<typeof useQueryClient>, classId: string, error: unknown) {
  if (error instanceof ApiError && error.status === 409 && error.body) {
    replace(queryClient, classId, error.body as CatchUpPackage)
  } else if (!(error instanceof ApiError && error.status === 400)) {
    void queryClient.invalidateQueries({ queryKey: packagesKey(classId) })
  }
}

/** Simpler / Shorter / Try again (hint null): a new version is written in the background. */
export function useRegenerate(classId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, hint }: { id: string; hint: 'simpler' | 'shorter' | null }) =>
      api<CatchUpPackage>(`/api/packages/${id}/regenerate`, {
        method: 'POST',
        body: { clientRequestId: crypto.randomUUID(), hint },
      }),
    onSuccess: (updated) => replace(queryClient, classId, updated),
    onError: () => void queryClient.invalidateQueries({ queryKey: packagesKey(classId) }),
  })
}
