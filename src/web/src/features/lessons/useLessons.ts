import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useCallback } from 'react'
import { api, NetworkError } from '../../api/client'
import { db, saveSnapshot } from '../../offline/db'
import { enqueue, flush, pendingFor } from '../../offline/outbox'
import type { Curriculum, LessonDay } from './types'

/** Server answer, saved for offline use; the snapshot is used when the network is down. */
async function withSnapshot<T>(key: string, load: () => Promise<T>): Promise<T> {
  try {
    const value = await load()
    await saveSnapshot(key, value)
    return value
  } catch (error) {
    const snapshot = error instanceof NetworkError ? await db.snapshots.get(key) : undefined
    if (!snapshot) {
      throw error
    }
    return snapshot.value as T
  }
}

export function useCurriculum() {
  return useQuery({
    queryKey: ['curriculum'],
    queryFn: () => withSnapshot('curriculum', () => api<Curriculum>('/api/curriculum')),
    staleTime: Infinity,
    retry: false,
  })
}

export const lessonKey = (classId: string, date: string) => ['lesson', classId, date] as const

const lessonUrl = (classId: string, date: string) => `/api/classes/${classId}/lessons/${date}`

type SaveBody = { clientRequestId: string; competencyIds: string[] }

const withPending = (day: LessonDay, pending: SaveBody | undefined): LessonDay =>
  pending
    ? { ...day, lesson: { competencyIds: pending.competencyIds, source: 'picked', status: 'confirmed' } }
    : day

/** The day's lesson (online, else the last snapshot), with any pick still in the outbox shown on top. */
export function useLessonDay(classId: string, date: string) {
  return useQuery({
    queryKey: lessonKey(classId, date),
    queryFn: async () => {
      const url = lessonUrl(classId, date)
      const day = await withSnapshot(lessonKey(classId, date).join(':'), () => api<LessonDay>(url))
      const pending = (await pendingFor(url)).map((item) => item.body as SaveBody).at(-1)
      return withPending(day, pending)
    },
    retry: false,
  })
}

/** Confirms the day's lesson(s) through the outbox: shown at once, sent now or when the network is back. */
export function useSaveLesson(classId: string, date: string) {
  const queryClient = useQueryClient()
  return useCallback(
    async (competencyIds: string[]) => {
      const body: SaveBody = { clientRequestId: crypto.randomUUID(), competencyIds }
      await enqueue({ id: body.clientRequestId, url: lessonUrl(classId, date), method: 'PUT', body })
      queryClient.setQueryData<LessonDay>(lessonKey(classId, date), (day) => day && withPending(day, body))
      if ((await flush()) > 0) {
        await queryClient.invalidateQueries({ queryKey: lessonKey(classId, date) })
      }
    },
    [classId, date, queryClient],
  )
}
