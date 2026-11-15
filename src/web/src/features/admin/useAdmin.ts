import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ApiError, NetworkError, api } from '../../api/client'
import type { TextbookPage } from '../lessons/types'

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

/** One lesson on the lesson links page (T-94), with its video link or null. */
export interface AdminLessonLink {
  id: string
  nameKn: string
  nameEn: string
  textbookPage: TextbookPage | null
  url: string | null
}

/** `GET /api/admin/lesson-links`: every chapter and lesson in book order. */
export interface AdminLessonLinks {
  chapters: { number: number; nameKn: string; nameEn: string; lessons: AdminLessonLink[] }[]
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

/**
 * The key after `#key=` in the page's link. It stays in the fragment, so it never reaches a server log. Null without
 * one: the API then answers only when it runs locally in Development (D-102), else 404.
 */
export function adminKey(hash: string): string | null {
  return new URLSearchParams(hash.replace(/^#/, '')).get('key') || null
}

/** The key header, or none without a key. */
function keyHeaders(key: string | null): Record<string, string> {
  return key === null ? {} : { [KEY_HEADER]: key }
}

/** Every greeting; a missing or wrong key is a 404 (T-66), except locally (D-102). */
export function useAdminGreetings(key: string | null) {
  return useQuery({
    queryKey: ['admin', 'greetings', key],
    queryFn: () => api<AdminGreeting[]>('/api/admin/greetings', { headers: keyHeaders(key) }),
    retry: false,
  })
}

/** The lesson audio of one day (T-67). */
export function useAdminLessonAudio(key: string | null, date: string) {
  return useQuery({
    queryKey: ['admin', 'lesson-audio', key, date],
    queryFn: () =>
      api<AdminLessonAudioDay>(`/api/admin/lesson-audio?date=${encodeURIComponent(date)}`, {
        headers: keyHeaders(key),
      }),
    enabled: date !== '',
    retry: false,
  })
}

/** A clip's audio as a blob: an `<audio src>` can't send the key header. */
export async function fetchAdminClip(key: string | null, url: string): Promise<Blob> {
  let response: Response
  try {
    response = await fetch(url, { headers: keyHeaders(key) })
  } catch {
    throw new NetworkError()
  }
  if (!response.ok) {
    throw new ApiError(response.status, null)
  }
  return response.blob()
}

/** Every chapter and lesson with its video link (T-94, D-106). */
export function useAdminLessonLinks(key: string | null) {
  return useQuery({
    queryKey: ['admin', 'lesson-links', key],
    queryFn: () => api<AdminLessonLinks>('/api/admin/lesson-links', { headers: keyHeaders(key) }),
    retry: false,
  })
}

/** Sets (any https link) or removes (blank) one lesson's link; a refused link is an `ApiError` with status 400. */
export function useSetLessonLink(key: string | null) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, url }: { id: string; url: string }) =>
      api<AdminLessonLink>(`/api/admin/lesson-links/${encodeURIComponent(id)}`, {
        method: 'PUT',
        headers: keyHeaders(key),
        body: { clientRequestId: crypto.randomUUID(), url },
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin', 'lesson-links', key] }),
  })
}

/** One child on the admin children page (T-118): the number their family's WhatsApp goes to, and where it comes from. */
export interface AdminChild {
  id: string
  rollNo: number
  firstName: string
  homeLanguage: string
  consent: 'yes' | 'no' | 'nophone'
  stopped: boolean
  phone: string | null
  phoneFrom: 'saved' | 'setting' | null
}

/** `GET /api/admin/children`: every class with its children by roll number. */
export interface AdminChildren {
  classes: { id: string; label: string; children: AdminChild[] }[]
}

/** Every class's children and their WhatsApp numbers (T-118, D-110). */
export function useAdminChildren(key: string | null) {
  return useQuery({
    queryKey: ['admin', 'children', key],
    queryFn: () => api<AdminChildren>('/api/admin/children', { headers: keyHeaders(key) }),
    retry: false,
  })
}

/** Saves (a phone number) or clears (blank) a family's number; a refused number is an `ApiError` with status 400. */
export function useSetParentPhone(key: string | null) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, phone }: { id: string; phone: string }) =>
      api<AdminChild>(`/api/admin/children/${encodeURIComponent(id)}/phone`, {
        method: 'PUT',
        headers: keyHeaders(key),
        body: { clientRequestId: crypto.randomUUID(), phone },
      }),
    onSuccess: (child) =>
      queryClient.setQueryData<AdminChildren>(['admin', 'children', key], (data) =>
        data
          ? {
              classes: data.classes.map((c) => ({
                ...c,
                children: c.children.map((existing) => (existing.id === child.id ? child : existing)),
              })),
            }
          : data,
      ),
  })
}
