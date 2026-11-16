/** Return speed for one school week (D-24): counts only, from one list of spells so they always add up. */
export type ReturnSpeed = {
  from: string
  to: string
  oneDay: number
  twoDays: number
  threeDays: number
  fourPlusDays: number
  backWithinTwoDays: number
  total: number
}

export type WeekReasons = {
  unwell: number
  travelOrWork: number
  festival: number
  neededAtHome: number
  other: number
  notGiven: number
}

/** FR-13, D-60: every number counted by code; the sentence is a fixed template (no AI). */
export type PacingNudge = {
  lessonId: string
  date: string
  competencyIds: string[]
  n: number
  total: number
  minutes: number
  nextCompetencyId: string | null
}

/** FR-24, D-118: present every school day of this finished week, or every day so far in the current one. No counts. */
export type StarChild = {
  childId: string
  rollNo: number
  firstName: string
}

export type WeekStars = {
  finished: boolean
  schoolDays: number
  children: StarChild[]
}

/** GET /api/classes/{id}/week/{date} */
export type Week = {
  from: string
  to: string
  empty: boolean
  thisWeek: ReturnSpeed
  lastWeek: ReturnSpeed
  reasons: WeekReasons
  nudge: PacingNudge | null
  decision: 'recap' | 'skip' | null
  /** Null when the week has no register. Missing in a week saved before stars existed. */
  stars?: WeekStars | null
  savedAt?: number
}
