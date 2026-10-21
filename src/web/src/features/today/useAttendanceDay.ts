import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useCallback } from 'react'
import { api, NetworkError } from '../../api/client'
import { loadSnapshot, saveSnapshot } from '../../offline/db'
import { enqueue, flush, pendingFor } from '../../offline/outbox'
import type { AttendanceDay } from './types'

export const attendanceKey = (classId: string, date: string) => ['attendance', classId, date] as const

const registerUrl = (classId: string) => `/api/classes/${classId}/attendance`

type RegisterBody = { clientRequestId: string; date: string; absentChildIds: string[] }

/** Her unsent register for the date, if any, shown on top of what the server last said. */
function withPending(day: AttendanceDay, pending: RegisterBody | undefined): AttendanceDay {
  if (!pending) {
    return day
  }
  const fromSats = day.status === 'sats'
  return {
    ...day,
    status: fromSats ? 'sats' : 'manual',
    corrected: fromSats,
    absentChildIds: pending.absentChildIds,
  }
}

/**
 * Today's register (D-41). Online it asks the server (which fetches the simulated SATS feed when due);
 * offline it shows the last snapshot. Either way, a register still in the outbox is shown on top.
 */
export function useAttendanceDay(classId: string, date: string) {
  return useQuery({
    queryKey: attendanceKey(classId, date),
    queryFn: async () => {
      const key = attendanceKey(classId, date).join(':')
      let day: AttendanceDay
      try {
        day = await api<AttendanceDay>(`${registerUrl(classId)}/${date}`)
        await saveSnapshot(key, day)
      } catch (error) {
        const snapshot = error instanceof NetworkError ? await loadSnapshot<AttendanceDay>(key) : null
        if (!snapshot) {
          throw error
        }
        day = snapshot
      }
      const pending = (await pendingFor(registerUrl(classId)))
        .map((item) => item.body as RegisterBody)
        .filter((body) => body.date === date)
        .at(-1)
      return withPending(day, pending)
    },
    retry: false,
  })
}

/** Saves her register through the outbox: shown at once, sent now or when the network is back. */
export function useSaveAttendance(classId: string, date: string) {
  const queryClient = useQueryClient()
  return useCallback(
    async (absentChildIds: string[]) => {
      const clientRequestId = crypto.randomUUID()
      const body: RegisterBody = { clientRequestId, date, absentChildIds }
      await enqueue({ id: clientRequestId, url: registerUrl(classId), body })
      queryClient.setQueryData<AttendanceDay>(attendanceKey(classId, date), (day) => day && withPending(day, body))
      const sent = await flush()
      if (sent > 0) {
        await queryClient.invalidateQueries({ queryKey: attendanceKey(classId, date) })
      }
    },
    [classId, date, queryClient],
  )
}
