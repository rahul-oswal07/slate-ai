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
}

export interface TodaysPicture {
  kind: 'compare'
  a: number
  b: number
  answer: '>' | '<' | '='
  removed: boolean
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
  note: { text: Record<string, string>; removed: boolean } | null
  removed: TodaysSectionKind[]
  languages: TodaysLanguage[]
  /** The book's pairs for the picture question; `pictureIndex` is the one in use. */
  pairs: number[][]
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
