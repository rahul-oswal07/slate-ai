import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useLiveQuery } from 'dexie-react-hooks'
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

type SaveBody = { clientRequestId: string; competencyIds: string[]; source?: 'voice' | 'picked' }

const withPending = (day: LessonDay, pending: SaveBody | undefined): LessonDay =>
  pending
    ? {
        ...day,
        lesson: {
          summaryKn: null,
          summaryEn: null,
          examplesUsed: [],
          ...day.lesson,
          competencyIds: pending.competencyIds,
          source: pending.source ?? 'picked',
          status: 'confirmed',
        },
      }
    : day

/**
 * The day's lesson (online, else the last snapshot), with any pick still in the outbox shown on top.
 * While a voice note is being matched it checks again every 2 s.
 */
export function useLessonDay(classId: string, date: string) {
  return useQuery({
    refetchInterval: (query) => (query.state.data?.lesson?.status === 'processing' ? 2000 : false),
    queryKey: lessonKey(classId, date),
    queryFn: async () => {
      const url = lessonUrl(classId, date)
      const day = await withSnapshot(lessonKey(classId, date).join(':'), () => api<LessonDay>(url))
      const pending = (await pendingFor(url)).map((item) => item.body as SaveBody | undefined).filter(Boolean).at(-1)
      return withPending(day, pending)
    },
    retry: false,
  })
}

/** Confirms the day's lesson(s) through the outbox: shown at once, sent now or when the network is back. */
export function useSaveLesson(classId: string, date: string) {
  const queryClient = useQueryClient()
  return useCallback(
    async (competencyIds: string[], source: 'voice' | 'picked' = 'picked') => {
      const body: SaveBody = { clientRequestId: crypto.randomUUID(), competencyIds, source }
      await enqueue({ id: body.clientRequestId, url: lessonUrl(classId, date), method: 'PUT', body })
      queryClient.setQueryData<LessonDay>(lessonKey(classId, date), (day) => day && withPending(day, body))
      // Her recording is only kept until the lesson is confirmed.
      await db.notes.delete(date)
      if ((await flush()) > 0) {
        await queryClient.invalidateQueries({ queryKey: lessonKey(classId, date) })
      }
    },
    [classId, date, queryClient],
  )
}

/** A voice note for this day still waiting in the outbox (T1 "queued"). */
export function useNoteQueued(classId: string, date: string) {
  const url = `${lessonUrl(classId, date)}/audio`
  return useLiveQuery(async () => (await pendingFor(url)).length > 0, [url], false)
}

/** Her recording for the day, for playback on T2 (kept on the phone only). */
export function useNote(date: string) {
  return useLiveQuery(() => db.notes.get(date), [date])
}

export type SendResult = 'sent' | 'queued'

/**
 * Keeps the note on the phone, queues the upload and tries to send it. Resolves to "sent" when the server
 * has it, or "queued" when it will go later; `onSlow` fires if it is still uploading after 3 s (D-34).
 */
export function useSendNote(classId: string, date: string) {
  const queryClient = useQueryClient()
  return useCallback(
    async (note: Blob, onSlow: () => void): Promise<SendResult> => {
      const id = crypto.randomUUID()
      const data = await note.arrayBuffer()
      const fileName = note.type.includes('mp4') ? 'note.m4a' : 'note.webm'
      await db.notes.put({ date, data, type: note.type, savedAt: Date.now() })
      await enqueue({
        id,
        url: `${lessonUrl(classId, date)}/audio`,
        body: undefined,
        audio: { data, type: note.type || 'audio/webm', fileName },
      })
      const slow = window.setTimeout(onSlow, 3000)
      try {
        await flush()
      } finally {
        window.clearTimeout(slow)
      }
      const stillQueued = (await db.outbox.get(id)) !== undefined
      await queryClient.invalidateQueries({ queryKey: lessonKey(classId, date) })
      return stillQueued ? 'queued' : 'sent'
    },
    [classId, date, queryClient],
  )
}
