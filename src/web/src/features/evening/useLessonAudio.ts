import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../../api/client'
import type { LessonAudioDay } from './types'

export const lessonAudioKey = (classId: string, date: string) => ['lesson-audio', classId, date] as const

/** The day's optional lesson audio. Polls while a clip is being made, as the catch-up list does. Online only. */
export function useLessonAudio(classId: string, date: string, enabled: boolean) {
  return useQuery({
    queryKey: lessonAudioKey(classId, date),
    queryFn: () => api<LessonAudioDay>(`/api/classes/${classId}/lesson-audio/${date}`),
    enabled: enabled && classId !== '',
    retry: false,
    refetchInterval: (query) => (query.state.data?.items.some((i) => i.status === 'generating') ? 3000 : false),
  })
}

/** Ticks or unticks the box, approves a clip, or asks for a new one: each tap is a new request. */
export function useLessonAudioActions(classId: string, date: string) {
  const queryClient = useQueryClient()
  const base = `/api/classes/${classId}/lesson-audio/${date}`
  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: lessonAudioKey(classId, date) })
    void queryClient.invalidateQueries({ queryKey: ['evening', classId] })
  }
  // A 409 means the day changed (sent, or a clip was replaced): show the latest.
  const options = {
    onSuccess: (day: LessonAudioDay) => {
      queryClient.setQueryData(lessonAudioKey(classId, date), day)
      refresh()
    },
    onError: refresh,
  }

  const tick = useMutation({
    mutationFn: (enabled: boolean) =>
      api<LessonAudioDay>(base, { method: 'PUT', body: { clientRequestId: crypto.randomUUID(), enabled } }),
    ...options,
  })
  const approve = useMutation({
    mutationFn: ({ language, clipId }: { language: string; clipId: string }) =>
      api<LessonAudioDay>(`${base}/${language}/approve`, {
        method: 'POST',
        body: { clientRequestId: crypto.randomUUID(), clipId },
      }),
    ...options,
  })
  const regenerate = useMutation({
    mutationFn: (language: string) =>
      api<LessonAudioDay>(`${base}/${language}/regenerate`, { method: 'POST', body: { clientRequestId: crypto.randomUUID() } }),
    ...options,
  })
  return { tick, approve, regenerate }
}
