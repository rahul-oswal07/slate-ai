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
