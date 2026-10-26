/** Textbook page for the homework (D-22), from the curriculum. */
export type Homework = { book: string; part: number | null; page: number }

export type ParentAudioSegment = { kind: 'greeting' | 'disclosure' | 'script'; url: string; seconds: number }

export type ParentOption = { id: number; number: number }

/** GET /api/p/{token} for a catch-up link. With `stopped`, the lesson fields are null (P4 stopped view). */
export type ParentCatchUp = {
  kind: 'catchUp'
  language: string
  childName: string
  teacherName: string
  stopped: boolean
  answered: boolean
  expiresAt: string
  homework: Homework | null
  audio: { segments: ParentAudioSegment[]; totalSeconds: number } | null
  transcript: { disclosureText: string; script: string; keyWordsKn: string[] } | null
  activity: { question: string; number: number; instruction: string | null; objectOption: string | null } | null
  check: { question: string; prompt: string | null; options: ParentOption[] } | null
}

/** The 410 body for an old catch-up link. */
export type ParentExpired = { kind: 'catchUp'; language: string; childName: string; teacherName: string; homework: Homework | null }

/** An evening-message link (shown in the demo chat later, T-18); the page only needs to recognise it. */
export type ParentEvening = {
  kind: 'evening'
  language: string
  childName: string
  teacherName: string
  text: string
  /** The lesson audio she approved for this day (T-62): disclosure, then the lesson; null if she sent none. */
  lessonAudio?: { segments: ParentAudioSegment[]; totalSeconds: number; disclosureText: string; transcript: string | null } | null
  /** Her recorded greeting, fetched by this link's token; null if she has none or the family stopped. */
  greeting: { url: string; seconds: number } | null
  stopped: boolean
  /** The reason the family already gave for this day (FR-21), or null. */
  reason: ReasonCategory | null
}

/** The five picture buttons on the reason page (FR-21, D-23): a category, never a diagnosis. */
export const REASON_CATEGORIES = ['unwell', 'travelOrWork', 'festival', 'neededAtHome', 'other'] as const
export type ReasonCategory = (typeof REASON_CATEGORIES)[number]
