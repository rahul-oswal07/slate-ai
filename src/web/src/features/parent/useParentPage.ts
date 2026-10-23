import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useRef } from 'react'
import { api } from '../../api/client'
import type { ParentCatchUp, ParentEvening, ReasonCategory } from './types'

export const parentKey = (token: string) => ['parent', token] as const

/** What the family's link opens. Online only (D-70); a 410 is the "link is old" view, a 404 "link doesn't work". */
export function useParentPage(token: string) {
  return useQuery({
    queryKey: parentKey(token),
    queryFn: () => api<ParentCatchUp | ParentEvening>(`/api/p/${token}`),
    retry: false,
  })
}

/**
 * Sends the picture the child chose. One clientRequestId per visit, so Try again after a dropped connection can't
 * store a second answer (the server keeps the first anyway, D-62). The response never says right or wrong.
 */
export function useAnswer(token: string) {
  const queryClient = useQueryClient()
  const requestId = useRef(crypto.randomUUID())
  return useMutation({
    mutationFn: (choiceId: number) =>
      api<{ answered: boolean }>(`/api/p/${token}/answer`, {
        method: 'POST',
        body: { choiceId, clientRequestId: requestId.current },
      }),
    onSuccess: () =>
      queryClient.setQueryData<ParentCatchUp | ParentEvening>(parentKey(token), (page) =>
        page?.kind === 'catchUp' ? { ...page, answered: true } : page,
      ),
  })
}

/** Stop these messages / Start them again (FR-19, D-19). Afterwards the page is fetched again. */
export function useStop(token: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (stop: boolean) =>
      api<{ stopped: boolean }>(`/api/p/${token}/${stop ? 'stop' : 'restart'}`, {
        method: 'POST',
        body: { clientRequestId: crypto.randomUUID() },
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: parentKey(token) }),
  })
}

/**
 * Sends why the child was away (FR-21). One clientRequestId per visit, so Try again can't count twice; a later tap
 * with a different picture replaces the reason on the server.
 */
export function useReason(token: string) {
  const queryClient = useQueryClient()
  const requestId = useRef(crypto.randomUUID())
  return useMutation({
    mutationFn: (category: ReasonCategory) =>
      api<{ category: ReasonCategory }>(`/api/p/${token}/reason`, {
        method: 'POST',
        body: { category, clientRequestId: requestId.current },
      }),
    onSuccess: ({ category }) => {
      // The next change of mind is a new request.
      requestId.current = crypto.randomUUID()
      queryClient.setQueryData<ParentCatchUp | ParentEvening>(parentKey(token), (page) =>
        page?.kind === 'evening' ? { ...page, reason: category } : page,
      )
    },
  })
}
