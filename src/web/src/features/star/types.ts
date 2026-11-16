import type { TextPart } from '../evening/types'

/** One language of the weekly star wording (T-125, D-118), filled with a child from her class. */
export type StarWording = { language: string; whatsApp: TextPart[]; message: TextPart[]; badge: TextPart[] }

export type WeeklyStarWording = {
  version: string
  /** Approved at this version; an older approval doesn't count (D-49). */
  approved: boolean
  languages: StarWording[]
}
