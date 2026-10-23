import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/client'
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
    mutationFn: ({ id, scriptVersion }: { id: string; scriptVersion: number }) =>
      api<CatchUpPackage>(`/api/packages/${id}/approve`, {
        method: 'POST',
        body: { clientRequestId: crypto.randomUUID(), scriptVersion },
      }),
    onSuccess: (updated) => replace(queryClient, classId, updated),
    onError: () => void queryClient.invalidateQueries({ queryKey: packagesKey(classId) }),
  })
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
