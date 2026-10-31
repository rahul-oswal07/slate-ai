import { useQuery } from '@tanstack/react-query'
import { ApiError, NetworkError, api } from '../../api/client'

/** One teacher's recorded greeting, as `GET /api/admin/greetings` lists it. */
export interface AdminGreeting {
  clipId: string
  teacherName: string
  seconds: number
  recordedAt: string
}

/** A family one lesson audio went to. */
export interface AdminFamily {
  childName: string
  rollNo: number
  language: string
  sentAt: string
}

/** One class's lesson audio in one language (T-62), sent or not. */
export interface AdminLessonAudio {
  id: string
  classLabel: string
  language: string
  lessons: string[]
  status: 'generating' | 'ready' | 'approved' | 'failed'
  verdict: string | null
  approvedBy: string | null
  approvedAt: string | null
  clipId: string | null
  seconds: number | null
  transcript: string | null
  families: AdminFamily[]
}

/** `GET /api/admin/lesson-audio?date=…` */
export interface AdminLessonAudioDay {
  date: string
  sentCount: number
  audios: AdminLessonAudio[]
}

const KEY_HEADER = 'X-Admin-Key'

/** "28 Oct 2026, 2:30 pm", in the UI language and school time. */
export function adminTime(at: string, language: string) {
  return new Intl.DateTimeFormat(language === 'en' ? 'en-IN' : language, {
    dateStyle: 'medium',
    timeStyle: 'short',
    timeZone: 'Asia/Kolkata',
  }).format(new Date(at))
}

/** The key after `#key=` in the page's link. It stays in the fragment, so it never reaches a server log. */
export function adminKey(hash: string): string | null {
  return new URLSearchParams(hash.replace(/^#/, '')).get('key') || null
}

/** Every greeting; a missing or wrong key is a 404 (T-66). */
export function useAdminGreetings(key: string) {
  return useQuery({
    queryKey: ['admin', 'greetings', key],
    queryFn: () => api<AdminGreeting[]>('/api/admin/greetings', { headers: { [KEY_HEADER]: key } }),
    retry: false,
  })
}

/** The lesson audio of one day (T-67). */
export function useAdminLessonAudio(key: string, date: string) {
  return useQuery({
    queryKey: ['admin', 'lesson-audio', key, date],
    queryFn: () =>
      api<AdminLessonAudioDay>(`/api/admin/lesson-audio?date=${encodeURIComponent(date)}`, {
        headers: { [KEY_HEADER]: key },
      }),
    enabled: date !== '',
    retry: false,
  })
}

/** A clip's audio as a blob: an `<audio src>` can't send the key header. */
export async function fetchAdminClip(key: string, url: string): Promise<Blob> {
  let response: Response
  try {
    response = await fetch(url, { headers: { [KEY_HEADER]: key } })
  } catch {
    throw new NetworkError()
  }
  if (!response.ok) {
    throw new ApiError(response.status, null)
  }
  return response.blob()
}
