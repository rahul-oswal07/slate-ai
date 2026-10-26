import type { AudioSegment } from '../approve/types'

/** A run of the evening message: fixed text, or a slot filled by code (underlined on T0). */
export type TextPart = { text: string; slot: 'child' | 'lesson' | 'page' | null }

export type MessageTemplate = {
  version: string
  status: 'draft' | 'approved'
  approvedAt: string | null
  languages: { language: string; example: TextPart[] }[]
}

export type EveningOutcome = 'willSend' | 'sent' | 'noConsent' | 'noPhone' | 'stopped' | 'noWording'

/** Tonight's evening messages (GET /api/classes/{id}/evening-messages/{date}). */
export type EveningDay = {
  date: string
  blocked: 'templateNotApproved' | 'lessonNotConfirmed' | 'noAttendance' | null
  templateApproved: boolean
  lessonConfirmed: boolean
  absent: number
  families: number
  sent: number
  sendsAt: string
  children: { childId: string; outcome: EveningOutcome }[]
}

export type LessonAudioStatus = 'generating' | 'ready' | 'approved' | 'failed'

/** One language's clip of the day's lesson audio (T-62): disclosure then lesson, played from `/api/audio/{id}`. */
export type LessonAudioItem = {
  language: 'kn' | 'te' | 'ta' | 'en' | 'hi'
  status: LessonAudioStatus
  children: number
  clipId: string | null
  segments: AudioSegment[]
  seconds: number
  transcript: string | null
  approvedAt: string | null
}

/** GET /api/classes/{id}/lesson-audio/{date}: `available` she can tick it; `enabled` ticked; `sent` read-only. */
export type LessonAudioDay = {
  date: string
  available: boolean
  enabled: boolean
  sent: boolean
  items: LessonAudioItem[]
}
