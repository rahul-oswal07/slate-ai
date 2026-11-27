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
  /** Their latest submitted picture (D-108), or null. */
  choiceId: number | null
  /** Both submits used: the answer page shows their choice with Next only. */
  locked: boolean
  expiresAt: string
  homework: Homework | null
  audio: { segments: ParentAudioSegment[]; totalSeconds: number } | null
  transcript: { disclosureText: string; script: string; keyWordsKn: string[] } | null
  activity: { question: string; number: number; instruction: string | null; objectOption: string | null } | null
  check: { question: string; prompt: string | null; options: ParentOption[] } | null
  /** Her note as she typed it (T-103), unless she removed it. */
  note?: string | null
  /** The lesson's video link (T-103), unless she removed it. */
  videoUrl?: string | null
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
  /** Today's message she approved (D-91, T-91), in this family's language; null or missing: the plain message. */
  todaysMessage?: ParentTodaysMessage | null
  /** Today's activity steps, voiced (T-120): disclosure, then the steps; null when there is no clip or the activity was removed. */
  activityAudio?: ParentEvening['lessonAudio']
}

/** A weekly star link (T-125, D-118): the texts she approved with the name filled in; none when the family stopped. */
export type ParentWeeklyStar = {
  kind: 'weeklyStar'
  language: string
  childName: string
  teacherName: string
  weekOf: string
  stopped: boolean
  message: string | null
  badge: string | null
}

/** A sign the child taps on the picture question (T-90). */
export type Sign = '>' | '<' | '='

/** What the child submits: a sign, or for a sum the number they chose, as text ("686", D-116). */
export type PictureAnswer = Sign | `${number}`

/** Today's message as the family gets it: removed sections are null. The picture never carries its answer. */
export type ParentTodaysMessage = {
  concept: string | null
  steps: string[] | null
  /** `done`: right, or two taps; the child can't tap again. */
  /** `locked`: both submits used (D-108); the page shows their latest choice with Next only. */
  /**
   * `kind` compare: the child taps >, < or = between a and b. add / sub (D-116): the child works out a + b or a − b and
   * picks one of `options`, drawn as sticks. Older links have no kind: compare.
   */
  picture: {
    a: number
    b: number
    kind?: 'compare' | 'add' | 'sub'
    options?: number[] | null
    firstAnswer: PictureAnswer | null
    secondAnswer: PictureAnswer | null
    locked: boolean
  } | null
  video: { url: string } | null
  note: string | null
}

/** The five picture buttons on the reason page (FR-21, D-23): a category, never a diagnosis. */
export const REASON_CATEGORIES = ['unwell', 'travelOrWork', 'festival', 'neededAtHome', 'other'] as const
export type ReasonCategory = (typeof REASON_CATEGORIES)[number]
