import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ApiError, NetworkError, api } from '../../api/client'
import type { Language } from '../../i18n'
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

/** "28 Oct 2026, 2:30 pm", in the UI language and school time. */
export function adminTime(at: string, language: string) {
  return new Intl.DateTimeFormat(language === 'en' ? 'en-IN' : language, {
    dateStyle: 'medium',
    timeStyle: 'short',
    timeZone: 'Asia/Kolkata',
  }).format(new Date(at))
}

/** Every greeting (T-66); no key needed (D-112). */
export function useAdminGreetings() {
  return useQuery({
    queryKey: ['admin', 'greetings'],
    queryFn: () => api<AdminGreeting[]>('/api/admin/greetings'),
    retry: false,
  })
}

/** The lesson audio of one day (T-67). */
export function useAdminLessonAudio(date: string) {
  return useQuery({
    queryKey: ['admin', 'lesson-audio', date],
    queryFn: () =>
      api<AdminLessonAudioDay>(`/api/admin/lesson-audio?date=${encodeURIComponent(date)}`),
    enabled: date !== '',
    retry: false,
  })
}

/** A clip's audio as a blob, so a failed fetch shows an error instead of a broken player. */
export async function fetchAdminClip(url: string): Promise<Blob> {
  let response: Response
  try {
    response = await fetch(url)
  } catch {
    throw new NetworkError()
  }
  if (!response.ok) {
    throw new ApiError(response.status, null)
  }
  return response.blob()
}

/** Every chapter and lesson with its video link (T-94, D-106). */
export function useAdminLessonLinks() {
  return useQuery({
    queryKey: ['admin', 'lesson-links'],
    queryFn: () => api<AdminLessonLinks>('/api/admin/lesson-links'),
    retry: false,
  })
}

/** Sets (any https link) or removes (blank) one lesson's link; a refused link is an `ApiError` with status 400. */
export function useSetLessonLink() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, url }: { id: string; url: string }) =>
      api<AdminLessonLink>(`/api/admin/lesson-links/${encodeURIComponent(id)}`, {
        method: 'PUT',
        body: { clientRequestId: crypto.randomUUID(), url },
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin', 'lesson-links'] }),
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
  /** The home language was changed on this page (T-122, D-113): it stays after a demo Reset. */
  languageChanged: boolean
}

/**
 * `GET /api/admin/children`: every class with its children by roll number. `demo` marks the demo class (with the demo
 * on), whose children each have a simulated family phone at `/demo/family/:childId` (T-130).
 */
export interface AdminChildren {
  classes: { id: string; label: string; demo: boolean; children: AdminChild[] }[]
}

/** Every class's children and their WhatsApp numbers (T-118, D-110). */
export function useAdminChildren() {
  return useQuery({
    queryKey: ['admin', 'children'],
    queryFn: () => api<AdminChildren>('/api/admin/children'),
    retry: false,
  })
}

/** Puts the child the API returned into the cached children list, in place. */
function useReplaceChild() {
  const queryClient = useQueryClient()
  return (child: AdminChild) =>
    queryClient.setQueryData<AdminChildren>(['admin', 'children'], (data) =>
      data
        ? {
            classes: data.classes.map((c) => ({
              ...c,
              children: c.children.map((existing) => (existing.id === child.id ? child : existing)),
            })),
          }
        : data,
    )
}

/** Saves (a phone number) or clears (blank) a family's number; a refused number is an `ApiError` with status 400. */
export function useSetParentPhone() {
  const replaceChild = useReplaceChild()
  return useMutation({
    mutationFn: ({ id, phone }: { id: string; phone: string }) =>
      api<AdminChild>(`/api/admin/children/${encodeURIComponent(id)}/phone`, {
        method: 'PUT',
        body: { clientRequestId: crypto.randomUUID(), phone },
      }),
    onSuccess: replaceChild,
  })
}

/** Changes a child's home language (T-122, D-113): one of `kn`, `en`, `te`, `ta`, `hi`. */
export function useSetHomeLanguage() {
  const replaceChild = useReplaceChild()
  return useMutation({
    mutationFn: ({ id, language }: { id: string; language: Language }) =>
      api<AdminChild>(`/api/admin/children/${encodeURIComponent(id)}/language`, {
        method: 'PUT',
        body: { clientRequestId: crypto.randomUUID(), language },
      }),
    onSuccess: replaceChild,
  })
}

/** One teacher on the admin teachers page (T-133): her voice consent and greeting. */
export interface AdminTeacher {
  id: string
  name: string
  /** "Shobha · Grade 3", or null when she has no class. */
  classLabel: string | null
  /** Her voice consent, the first-run gate; null: at her next visit she sees the Welcome and records her greeting. */
  consentAt: string | null
  greetingSeconds: number | null
}

/** Every teacher with her voice consent and greeting (T-133). */
export function useAdminTeachers() {
  return useQuery({
    queryKey: ['admin', 'teachers'],
    queryFn: () => api<AdminTeacher[]>('/api/admin/teachers'),
    retry: false,
  })
}

/** Clears a teacher's voice consent and deletes her greeting (T-133, D-44); the card updates in place. */
export function useClearConsent() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) =>
      api<AdminTeacher>(`/api/admin/teachers/${encodeURIComponent(id)}/clear-consent`, {
        method: 'POST',
        body: { clientRequestId: crypto.randomUUID() },
      }),
    onSuccess: (teacher) =>
      queryClient.setQueryData<AdminTeacher[]>(['admin', 'teachers'], (data) =>
        data?.map((existing) => (existing.id === teacher.id ? teacher : existing)),
      ),
  })
}
