/** GET /api/classes/{id}/attendance/{date} (D-41). */
export type AttendanceStatus = 'sats' | 'manual' | 'waiting' | 'noSchool'

export type RosterChild = { id: string; rollNo: number; firstName: string }

export type AttendanceDay = {
  date: string
  status: AttendanceStatus
  receivedAt: string | null
  corrected: boolean
  satsDiffers: boolean
  /** "HH:mm:ss" IST while waiting, else null. */
  recheckAt: string | null
  absentChildIds: string[]
  children: RosterChild[]
}
