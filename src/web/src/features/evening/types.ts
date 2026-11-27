/** A run of the evening message: fixed text, or a slot filled by code (underlined on T0). */
export type TextPart = { text: string; slot: 'child' | 'lesson' | 'page' | null }

export type MessageTemplate = {
  version: string
  status: 'draft' | 'approved'
  approvedAt: string | null
  languages: { language: string; example: TextPart[] }[]
}

export type EveningOutcome = 'willSend' | 'sent' | 'noConsent' | 'noPhone' | 'stopped' | 'noWording'

/** One absent child tonight: what happens, in which language, and whether today's message goes with it (T-86). */
export type EveningChild = { childId: string; outcome: EveningOutcome; language: string; withTodays: boolean }

/** What the 6 pm job waits for (T-86): today's message being written, or her approval of it. */
export type EveningWaiting = 'writing' | 'forYou'

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
  children: EveningChild[]
  /** When the job sends if she does nothing, worked out by the job's own rule (T-86); null when nothing is due. */
  goesAt: string | null
  waiting: EveningWaiting | null
  /** When the first message went, or null. */
  sentAt: string | null
  /** Today's message goes (approved) or went with the messages. */
  withTodays: boolean
}

/** One family's real message, for her Preview on Today (T-86). Read only. */
export type EveningPreview = {
  childId: string
  language: string
  text: string
  withTodays: boolean
  sent: boolean
  sentAt: string | null
}
