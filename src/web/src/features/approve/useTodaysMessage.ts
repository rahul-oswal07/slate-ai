import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api, ApiError } from '../../api/client'
import type { TodaysEditProblem, TodaysMessage, TodaysSectionKind } from './todaysTypes'

export const todaysKey = (classId: string, date: string) => ['todays-message', classId, date] as const

const url = (classId: string, date: string) => `/api/classes/${classId}/todays-message/${date}`

/**
 * Today's message (T-89). null: none yet (204). A 404 is a real error (not her class), never "none yet". Checks again
 * every 2 s while a new version is being written.
 */
export function useTodaysMessage(classId: string, date: string) {
  return useQuery({
    queryKey: todaysKey(classId, date),
    queryFn: async () => (await api<TodaysMessage | undefined>(url(classId, date))) ?? null,
    enabled: classId !== '',
    retry: false,
    refetchInterval: (query) => (query.state.data?.status === 'generating' ? 2000 : false),
  })
}

/** The 400 body of a refused edit, read as the problem she sees. */
export function editProblem(error: unknown): TodaysEditProblem | null {
  if (!(error instanceof ApiError) || error.status !== 400) {
    return null
  }
  const errors = (error.body as { errors?: Record<string, string[]> } | null)?.errors
  return errors?.problem?.[0]
    ? { problem: errors.problem[0], detail: errors.detail?.[0] ?? '', allowed: errors.allowed ?? [] }
    : null
}

/**
 * Every change goes through one mutation shape: the API answers with the latest message, also on a 409 (an older
 * version, or it was sent meanwhile), so the screen always shows what is current (D-68).
 */
function useChange<TVars>(classId: string, date: string, request: (vars: TVars) => Promise<TodaysMessage>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: request,
    onSuccess: (latest) => queryClient.setQueryData(todaysKey(classId, date), latest),
    onError: (error) => {
      if (error instanceof ApiError && error.status === 409 && error.body) {
        queryClient.setQueryData(todaysKey(classId, date), error.body as TodaysMessage)
      }
    },
  })
}

const body = (extra: object) => ({ method: 'POST' as const, body: { clientRequestId: crypto.randomUUID(), ...extra } })

export const useApproveTodays = (classId: string, date: string) =>
  useChange(classId, date, (version: number) => api<TodaysMessage>(`${url(classId, date)}/approve`, body({ version })))

export const useRetryTodays = (classId: string, date: string) =>
  useChange(classId, date, () => api<TodaysMessage>(`${url(classId, date)}/generate`, body({})))

export const useSetRemoved = (classId: string, date: string) =>
  useChange(classId, date, ({ kind, removed }: { kind: TodaysSectionKind; removed: boolean }) =>
    api<TodaysMessage>(`${url(classId, date)}/sections/${kind}`, { ...body({ removed }), method: 'PATCH' }),
  )

export const useEditSection = (classId: string, date: string) =>
  useChange(
    classId,
    date,
    ({ kind, version, text, steps }: { kind: 'concept' | 'activity' | 'note'; version: number; text?: string; steps?: string[] }) =>
      api<TodaysMessage>(`${url(classId, date)}/sections/${kind}`, { ...body({ version, text, steps }), method: 'PUT' }),
  )

export const useSetPicture = (classId: string, date: string) =>
  useChange(classId, date, ({ version, index }: { version: number; index: number }) =>
    api<TodaysMessage>(`${url(classId, date)}/picture`, { ...body({ version, index }), method: 'PUT' }),
  )
