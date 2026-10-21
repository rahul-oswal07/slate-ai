/** GET /api/classes/{id}/attendance/{date} (D-69). */
export type AttendanceStatus = 'sats' | 'manual' | 'waiting' | 'missing' | 'noSchool'

export type RosterChild = { id: string; rollNo: number; firstName: string }

export type AttendanceDay = {
  date: string
  status: AttendanceStatus
  receivedAt: string | null
  corrected: boolean
  satsDiffers: boolean
  /** "HH:mm:ss" IST while waiting: when it comes in, and the retry if late (D-45). */
  syncAt: string | null
  retryAt: string | null
  absentChildIds: string[]
  children: RosterChild[]
  /** When this copy was saved on the phone, if it came from the offline snapshot (D-68). */
  savedAt?: number
}
