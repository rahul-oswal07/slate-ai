import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useRef } from 'react'
import { api } from '../../api/client'
import type { WeeklyStarWording } from './types'

export const starWordingKey = (classId: string) => ['weekly-star', classId] as const

/** The weekly star wording she approves once (T-125). Online only (D-70). */
export function useWeeklyStarWording(classId: string) {
  return useQuery({
    queryKey: starWordingKey(classId),
    queryFn: () => api<WeeklyStarWording>(`/api/classes/${classId}/weekly-star`),
    enabled: classId !== '',
    retry: false,
  })
}

/** Approves it. One request ID per screen visit, so a retry is the same request; new after a 409. */
export function useApproveWeeklyStar(classId: string) {
  const queryClient = useQueryClient()
  const clientRequestId = useRef(crypto.randomUUID())
  return useMutation({
    mutationFn: (version: string) =>
      api<WeeklyStarWording>(`/api/classes/${classId}/weekly-star/approve`, {
        method: 'POST',
        body: { clientRequestId: clientRequestId.current, version },
      }),
    onSuccess: (wording) => queryClient.setQueryData(starWordingKey(classId), wording),
    onError: () => {
      // 409: the wording changed (D-49). Show the latest; a new tap is a new request.
      clientRequestId.current = crypto.randomUUID()
      void queryClient.invalidateQueries({ queryKey: starWordingKey(classId) })
    },
  })
}
