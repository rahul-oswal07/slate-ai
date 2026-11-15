/** GET /api/classes/{id}/todays-message/{date} (T-88, T-89). Numbers come from code; text from A9 or her own Kannada. */
export type TodaysStatus = 'generating' | 'ready' | 'approved' | 'failed'

export type TodaysSectionKind = 'concept' | 'activity' | 'picture' | 'video' | 'note'

export interface TodaysLanguage {
  language: string
  /** Families who get it in this language tonight. */
  families: number
  concept: string
  conceptClipUrl: string | null
  steps: string[]
  /** The steps voiced (T-120); null when too long to voice or the draft is older. */
  activityClipUrl?: string | null
}

/** compare: >, < or = between a and b. add / sub (D-116): a + b or a − b, picked from `options`; `answer` is the number as text. */
export interface TodaysPicture {
  kind: 'compare' | 'add' | 'sub'
  a: number
  b: number
  answer: string
  removed: boolean
  options?: number[] | null
}

export interface TodaysMessage {
  date: string
  status: TodaysStatus
  /** She approves this version; any edit makes a new one and clears her tick (D-92). */
  version: number
  approvedVersion: number | null
  approvedAt: string | null
  /** Families have it: read only. */
  sent: boolean
  verdict: string | null
  competencyIds: string[]
  picture: TodaysPicture | null
  video: { url: string; removed: boolean } | null
  /** `original`: what she typed, in any language (T-119); `text`: what each family reads. Older notes have no original. */
  note: { text: Record<string, string>; removed: boolean; original?: string | null } | null
  removed: TodaysSectionKind[]
  languages: TodaysLanguage[]
  /** The book's pairs for the picture question; `pictureIndex` is the one in use. */
  pairs: [number, number][]
  pictureIndex: number | null
  /** The Kannada is hers. */
  edited: boolean
}

/** Why her text wasn't saved (400 from the edit endpoint): the check's code and the numbers she may use. */
export interface TodaysEditProblem {
  problem: string
  detail: string
  allowed: string[]
}
