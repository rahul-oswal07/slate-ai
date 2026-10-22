import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useRef } from 'react'
import { api } from '../../api/client'
import type { EveningDay, MessageTemplate } from './types'

export const templateKey = (classId: string) => ['message-template', classId] as const
export const eveningKey = (classId: string, date: string) => ['evening', classId, date] as const

/** The evening-message template filled with an example (T0 step 2). Online only (D-70). */
export function useMessageTemplate(classId: string) {
  return useQuery({
    queryKey: templateKey(classId),
    queryFn: () => api<MessageTemplate>(`/api/classes/${classId}/message-template`),
    enabled: classId !== '',
    retry: false,
  })
}

/** Tonight's counts for the T1 tonight card. Online only; hidden when it can't load. */
export function useEveningDay(classId: string, date: string, enabled: boolean) {
  return useQuery({
    queryKey: eveningKey(classId, date),
    queryFn: () => api<EveningDay>(`/api/classes/${classId}/evening-messages/${date}`),
    enabled: enabled && classId !== '',
    retry: false,
  })
}

/** Approves the template once. One request ID per screen visit, so a retry is the same request. */
export function useApproveTemplate(classId: string) {
  const queryClient = useQueryClient()
  const clientRequestId = useRef(crypto.randomUUID())
  return useMutation({
    mutationFn: (version: string) =>
      api<MessageTemplate>(`/api/classes/${classId}/message-template/approve`, {
        method: 'POST',
        body: { clientRequestId: clientRequestId.current, version },
      }),
    onSuccess: (template) => {
      queryClient.setQueryData(templateKey(classId), template)
      void queryClient.invalidateQueries({ queryKey: ['evening', classId] })
    },
    onError: () => {
      // 409: the wording changed (D-49). Show the latest; a new tap is a new request.
      clientRequestId.current = crypto.randomUUID()
      void queryClient.invalidateQueries({ queryKey: templateKey(classId) })
    },
  })
}
